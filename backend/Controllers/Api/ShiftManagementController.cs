using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ShiftManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ShiftManagementController> _logger;

        public ShiftManagementController(ApplicationDbContext context, ILogger<ShiftManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("current-shift")]
        public async Task<IActionResult> GetCurrentShift()
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                var currentShift = await _context.ShiftAssignments
                    .Include(sa => sa.Shift)
                    .Include(sa => sa.User)
                    .Where(sa => sa.UserId == userId && 
                                 sa.TenantId == tenantId &&
                                 sa.AssignmentDate.Date == DateTime.UtcNow.Date &&
                                 (sa.AssignmentStatus == "InProgress" || 
                                  (sa.AssignmentStatus == "Scheduled" && 
                                   DateTime.UtcNow.TimeOfDay >= sa.ScheduledStart?.TimeOfDay)))
                    .FirstOrDefaultAsync();

                if (currentShift == null)
                {
                    return Ok(new
                    {
                        status = "inactive",
                        timeRange = "",
                        duration = "",
                        position = ""
                    });
                }

                var duration = currentShift.ActualStart.HasValue
                    ? (DateTime.UtcNow - currentShift.ActualStart.Value).TotalHours.ToString("F1")
                    : currentShift.Shift?.ScheduledDuration.HasValue
                        ? (currentShift.Shift.ScheduledDuration.Value / 60.0).ToString("F1")
                        : "0";

                return Ok(new
                {
                    status = currentShift.AssignmentStatus == "InProgress" ? "active" : "scheduled",
                    timeRange = $"{currentShift.ScheduledStart?.ToString("HH:mm")} - {currentShift.ScheduledEnd?.ToString("HH:mm")}",
                    duration = duration,
                    position = currentShift.Position ?? currentShift.Shift?.ShiftName ?? "Staff",
                    shiftId = currentShift.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current shift");
                return StatusCode(500, new { error = "Failed to get current shift" });
            }
        }

        [HttpGet("shifts")]
        public async Task<IActionResult> GetShifts([FromQuery] string period = "current")
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();
                var today = DateTime.UtcNow.Date;

                DateTime startDate = period switch
                {
                    "next" => today.AddDays(7 - (int)today.DayOfWeek),
                    "previous" => today.AddDays(-7 - (int)today.DayOfWeek),
                    _ => today.AddDays(-(int)today.DayOfWeek)
                };

                var endDate = startDate.AddDays(7);

                var shifts = await _context.ShiftAssignments
                    .Include(sa => sa.Shift)
                    .Where(sa => sa.UserId == userId &&
                                 sa.TenantId == tenantId &&
                                 sa.AssignmentDate >= startDate &&
                                 sa.AssignmentDate < endDate)
                    .OrderBy(sa => sa.AssignmentDate)
                    .ThenBy(sa => sa.ScheduledStart)
                    .ToListAsync();

                var result = shifts.Select(sa => new
                {
                    id = sa.Id,
                    day = sa.AssignmentDate.ToString("dddd"),
                    date = sa.AssignmentDate.ToString("yyyy-MM-dd"),
                    startTime = sa.ScheduledStart?.ToString("HH:mm") ?? sa.Shift?.ScheduledStart?.ToString() ?? "00:00",
                    endTime = sa.ScheduledEnd?.ToString("HH:mm") ?? sa.Shift?.ScheduledEnd?.ToString() ?? "00:00",
                    duration = sa.Shift?.ScheduledDuration.HasValue 
                        ? (sa.Shift.ScheduledDuration.Value / 60.0) 
                        : sa.TotalWorkedMinutes.HasValue 
                            ? (sa.TotalWorkedMinutes.Value / 60.0) 
                            : 8,
                    position = sa.Position ?? sa.Shift?.ShiftName ?? "Staff",
                    status = GetShiftStatus(sa),
                    notes = sa.Notes ?? "",
                    isToday = sa.AssignmentDate.Date == today
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shifts");
                return StatusCode(500, new { error = "Failed to get shifts" });
            }
        }

        [HttpPost("end-shift")]
        public async Task<IActionResult> EndShift([FromBody] EndShiftRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                var currentShift = await _context.ShiftAssignments
                    .Where(sa => sa.Id == request.ShiftId &&
                                 sa.UserId == userId &&
                                 sa.TenantId == tenantId &&
                                 sa.AssignmentStatus == "InProgress")
                    .FirstOrDefaultAsync();

                if (currentShift == null)
                {
                    return BadRequest(new { error = "No active shift found" });
                }

                currentShift.ActualEnd = DateTime.UtcNow;
                currentShift.AssignmentStatus = "Completed";
                
                if (currentShift.ActualStart.HasValue)
                {
                    var workedMinutes = (int)(currentShift.ActualEnd.Value - currentShift.ActualStart.Value).TotalMinutes;
                    currentShift.TotalWorkedMinutes = workedMinutes;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Shift ended successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending shift");
                return StatusCode(500, new { error = "Failed to end shift" });
            }
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetShiftSummary()
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();
                var today = DateTime.UtcNow.Date;
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                var endOfWeek = startOfWeek.AddDays(7);

                // Today's summary
                var todayShifts = await _context.ShiftAssignments
                    .Include(sa => sa.Shift)
                    .Where(sa => sa.UserId == userId &&
                                 sa.TenantId == tenantId &&
                                 sa.AssignmentDate.Date == today)
                    .ToListAsync();

                var todayHours = todayShifts
                    .Where(sa => sa.TotalWorkedMinutes.HasValue)
                    .Sum(sa => sa.TotalWorkedMinutes.Value) / 60.0;

                var todayCompleted = todayShifts.Count(sa => sa.AssignmentStatus == "Completed");
                var nextShift = todayShifts
                    .Where(sa => sa.AssignmentStatus == "Scheduled" && 
                                 sa.ScheduledStart > DateTime.UtcNow)
                    .OrderBy(sa => sa.ScheduledStart)
                    .FirstOrDefault();

                // Week overview
                var weekShifts = await _context.ShiftAssignments
                    .Include(sa => sa.Shift)
                    .Where(sa => sa.UserId == userId &&
                                 sa.TenantId == tenantId &&
                                 sa.AssignmentDate >= startOfWeek &&
                                 sa.AssignmentDate < endOfWeek)
                    .ToListAsync();

                var weekHours = weekShifts
                    .Where(sa => sa.Shift?.ScheduledDuration.HasValue == true)
                    .Sum(sa => sa.Shift.ScheduledDuration.Value) / 60.0;

                var overtime = weekHours > 40 ? weekHours - 40 : 0;

                return Ok(new
                {
                    todaySummary = new
                    {
                        totalHours = Math.Round(todayHours, 1),
                        shiftsCompleted = todayCompleted,
                        nextShift = nextShift != null 
                            ? $"{nextShift.ScheduledStart?.ToString("HH:mm")} - {nextShift.ScheduledEnd?.ToString("HH:mm")}"
                            : "None"
                    },
                    weekOverview = new
                    {
                        totalShifts = weekShifts.Count,
                        scheduledHours = Math.Round(weekHours, 1),
                        overtime = Math.Round(overtime, 1)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shift summary");
                return StatusCode(500, new { error = "Failed to get shift summary" });
            }
        }

        [HttpPost("request-time-off")]
        public async Task<IActionResult> RequestTimeOff([FromBody] TimeOffRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                // This would create a time off request in a real implementation
                // For now, return a success message
                return Ok(new { message = "Time off request submitted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting time off");
                return StatusCode(500, new { error = "Failed to submit time off request" });
            }
        }

        [HttpPost("swap-shift")]
        public async Task<IActionResult> SwapShift([FromBody] SwapShiftRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var tenantId = GetCurrentTenantId();

                // This would create a shift swap request in a real implementation
                // For now, return a success message
                return Ok(new { message = "Shift swap request submitted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error swapping shift");
                return StatusCode(500, new { error = "Failed to submit shift swap request" });
            }
        }

        private string GetCurrentUserId()
        {
            // This would extract user ID from JWT token in a real implementation
            return User.FindFirst("sub")?.Value ?? "demo-user";
        }

        private string GetCurrentTenantId()
        {
            // This would extract tenant ID from JWT token in a real implementation
            return User.FindFirst("tenant_id")?.Value ?? "demo-tenant";
        }

        private string GetShiftStatus(ShiftAssignment shift)
        {
            var now = DateTime.UtcNow;
            var shiftDate = shift.AssignmentDate.Date;
            var scheduledStart = shift.ScheduledStart ?? shift.AssignmentDate.Date.Add(shift.Shift?.ScheduledStart?.TimeOfDay ?? TimeSpan.Zero);
            var scheduledEnd = shift.ScheduledEnd ?? shift.AssignmentDate.Date.Add(shift.Shift?.ScheduledEnd?.TimeOfDay ?? TimeSpan.Zero);

            if (shift.AssignmentStatus == "Completed")
                return "completed";
            if (shift.AssignmentStatus == "InProgress")
                return "active";
            if (shiftDate > now.Date)
                return "upcoming";
            if (shiftDate == now.Date && now.TimeOfDay < scheduledStart.TimeOfDay)
                return "upcoming";
            if (shiftDate == now.Date && now.TimeOfDay >= scheduledStart.TimeOfDay && now.TimeOfDay < scheduledEnd.TimeOfDay)
                return "active";
            if (shiftDate < now.Date || (shiftDate == now.Date && now.TimeOfDay >= scheduledEnd.TimeOfDay))
                return "completed";

            return "scheduled";
        }
    }

    public class EndShiftRequest
    {
        public int ShiftId { get; set; }
    }

    public class TimeOffRequest
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; }
    }

    public class SwapShiftRequest
    {
        public int ShiftId { get; set; }
        public int TargetUserId { get; set; }
        public string Reason { get; set; }
    }
}
