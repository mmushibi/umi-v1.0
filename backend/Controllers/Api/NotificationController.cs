using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using UmiHealthPOS.DTOs;
using System.Security.Claims;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "superadmin")]
    public class NotificationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            ApplicationDbContext context,
            ILogger<NotificationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<NotificationDto>>> GetNotifications([FromQuery] NotificationFilterDto filter)
        {
            try
            {
                var query = _context.Notifications
                    .Include(n => n.User)
                    .Include(n => n.Tenant)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(filter.Type))
                {
                    query = query.Where(n => n.Type == filter.Type);
                }

                if (!string.IsNullOrEmpty(filter.Category))
                {
                    query = query.Where(n => n.Category == filter.Category);
                }

                if (filter.IsRead.HasValue)
                {
                    query = query.Where(n => n.IsRead == filter.IsRead.Value);
                }

                if (filter.IsGlobal.HasValue)
                {
                    query = query.Where(n => n.IsGlobal == filter.IsGlobal.Value);
                }

                if (!string.IsNullOrEmpty(filter.Search))
                {
                    query = query.Where(n =>
                        n.Title.Contains(filter.Search) ||
                        n.Message.Contains(filter.Search));
                }

                if (filter.StartDate.HasValue)
                {
                    query = query.Where(n => n.CreatedAt >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    query = query.Where(n => n.CreatedAt <= filter.EndDate.Value);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(n => new NotificationDto
                    {
                        Id = n.Id,
                        Type = n.Type,
                        Title = n.Title,
                        Message = n.Message,
                        Category = n.Category,
                        UserId = n.UserId,
                        TenantId = n.TenantId,
                        IsRead = n.IsRead,
                        IsGlobal = n.IsGlobal,
                        CreatedAt = n.CreatedAt,
                        UpdatedAt = n.UpdatedAt,
                        ReadAt = n.ReadAt,
                        ExpiresAt = n.ExpiresAt,
                        ActionUrl = n.ActionUrl,
                        ActionText = n.ActionText,
                        Metadata = n.Metadata
                    })
                    .ToListAsync();

                return Ok(new PagedResult<NotificationDto>
                {
                    Data = notifications,
                    TotalCount = totalCount,
                    Page = filter.Page,
                    PageSize = filter.PageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("stats")]
        public async Task<ActionResult<NotificationStatsDto>> GetNotificationStats()
        {
            try
            {
                var query = _context.Notifications.AsQueryable();

                var totalNotifications = await query.CountAsync();
                var unreadNotifications = await query.CountAsync(n => !n.IsRead);
                var readNotifications = await query.CountAsync(n => n.IsRead);
                var globalNotifications = await query.CountAsync(n => n.IsGlobal);
                var userNotifications = await query.CountAsync(n => !string.IsNullOrEmpty(n.UserId) && !n.IsGlobal);
                var tenantNotifications = await query.CountAsync(n => !string.IsNullOrEmpty(n.TenantId) && !n.IsGlobal);
                var expiredNotifications = await query.CountAsync(n => n.ExpiresAt.HasValue && n.ExpiresAt < DateTime.UtcNow);

                return Ok(new NotificationStatsDto
                {
                    TotalNotifications = totalNotifications,
                    UnreadNotifications = unreadNotifications,
                    ReadNotifications = readNotifications,
                    GlobalNotifications = globalNotifications,
                    UserNotifications = userNotifications,
                    TenantNotifications = tenantNotifications,
                    ExpiredNotifications = expiredNotifications
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification stats");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationDto>> GetNotification(int id)
        {
            try
            {
                var notification = await _context.Notifications
                    .Include(n => n.User)
                    .Include(n => n.Tenant)
                    .FirstOrDefaultAsync(n => n.Id == id);

                if (notification == null)
                {
                    return NotFound(new { error = "Notification not found" });
                }

                return Ok(new NotificationDto
                {
                    Id = notification.Id,
                    Type = notification.Type,
                    Title = notification.Title,
                    Message = notification.Message,
                    Category = notification.Category,
                    UserId = notification.UserId,
                    TenantId = notification.TenantId,
                    IsRead = notification.IsRead,
                    IsGlobal = notification.IsGlobal,
                    CreatedAt = notification.CreatedAt,
                    UpdatedAt = notification.UpdatedAt,
                    ReadAt = notification.ReadAt,
                    ExpiresAt = notification.ExpiresAt,
                    ActionUrl = notification.ActionUrl,
                    ActionText = notification.ActionText,
                    Metadata = notification.Metadata
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<NotificationDto>> CreateNotification([FromBody] CreateNotificationRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var notification = new Notification
                {
                    Type = request.Type,
                    Title = request.Title,
                    Message = request.Message,
                    Category = request.Category,
                    UserId = request.UserId,
                    TenantId = request.TenantId,
                    IsRead = false,
                    IsGlobal = request.IsGlobal,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ExpiresAt = request.ExpiresAt,
                    ActionUrl = request.ActionUrl,
                    ActionText = request.ActionText,
                    Metadata = request.Metadata
                };

                await _context.Notifications.AddAsync(notification);
                await _context.SaveChangesAsync();

                var notificationDto = new NotificationDto
                {
                    Id = notification.Id,
                    Type = notification.Type,
                    Title = notification.Title,
                    Message = notification.Message,
                    Category = notification.Category,
                    UserId = notification.UserId,
                    TenantId = notification.TenantId,
                    IsRead = notification.IsRead,
                    IsGlobal = notification.IsGlobal,
                    CreatedAt = notification.CreatedAt,
                    UpdatedAt = notification.UpdatedAt,
                    ReadAt = notification.ReadAt,
                    ExpiresAt = notification.ExpiresAt,
                    ActionUrl = notification.ActionUrl,
                    ActionText = notification.ActionText,
                    Metadata = notification.Metadata
                };

                return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notificationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<NotificationDto>> UpdateNotification(int id, [FromBody] UpdateNotificationRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                {
                    return NotFound(new { error = "Notification not found" });
                }

                notification.Type = request.Type;
                notification.Title = request.Title;
                notification.Message = request.Message;
                notification.Category = request.Category;
                notification.ExpiresAt = request.ExpiresAt;
                notification.ActionUrl = request.ActionUrl;
                notification.ActionText = request.ActionText;
                notification.Metadata = request.Metadata;
                notification.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var notificationDto = new NotificationDto
                {
                    Id = notification.Id,
                    Type = notification.Type,
                    Title = notification.Title,
                    Message = notification.Message,
                    Category = notification.Category,
                    UserId = notification.UserId,
                    TenantId = notification.TenantId,
                    IsRead = notification.IsRead,
                    IsGlobal = notification.IsGlobal,
                    CreatedAt = notification.CreatedAt,
                    UpdatedAt = notification.UpdatedAt,
                    ReadAt = notification.ReadAt,
                    ExpiresAt = notification.ExpiresAt,
                    ActionUrl = notification.ActionUrl,
                    ActionText = notification.ActionText,
                    Metadata = notification.Metadata
                };

                return Ok(notificationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("{id}/read")]
        public async Task<ActionResult> MarkNotificationAsRead(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                {
                    return NotFound(new { error = "Notification not found" });
                }

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                notification.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("{id}/unread")]
        public async Task<ActionResult> MarkNotificationAsUnread(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                {
                    return NotFound(new { error = "Notification not found" });
                }

                notification.IsRead = false;
                notification.ReadAt = null;
                notification.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Notification marked as unread" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as unread");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteNotification(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                {
                    return NotFound(new { error = "Notification not found" });
                }

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Notification deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("batch")]
        public async Task<ActionResult> BatchNotificationOperations([FromBody] NotificationBatchRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var notifications = await _context.Notifications
                    .Where(n => request.NotificationIds.Contains(n.Id))
                    .ToListAsync();

                if (notifications.Count == 0)
                {
                    return NotFound(new { error = "No notifications found" });
                }

                switch (request.Action.ToLower())
                {
                    case "mark_read":
                        foreach (var notification in notifications)
                        {
                            notification.IsRead = true;
                            notification.ReadAt = DateTime.UtcNow;
                            notification.UpdatedAt = DateTime.UtcNow;
                        }
                        break;

                    case "mark_unread":
                        foreach (var notification in notifications)
                        {
                            notification.IsRead = false;
                            notification.ReadAt = null;
                            notification.UpdatedAt = DateTime.UtcNow;
                        }
                        break;

                    case "delete":
                        _context.Notifications.RemoveRange(notifications);
                        break;

                    default:
                        return BadRequest(new { error = "Invalid batch action" });
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Batch {request.Action} completed successfully",
                    affectedCount = notifications.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing batch notification operations");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("cleanup")]
        public async Task<ActionResult> CleanupOldNotifications([FromQuery] int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var notificationsToDelete = await _context.Notifications
                    .Where(n => n.CreatedAt < cutoffDate)
                    .CountAsync();

                if (notificationsToDelete > 0)
                {
                    _context.Notifications.RemoveRange(
                        _context.Notifications.Where(n => n.CreatedAt < cutoffDate));
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    message = $"Cleaned up {notificationsToDelete} notifications older than {daysToKeep} days",
                    deletedCount = notificationsToDelete
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up notifications");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("export")]
        public async Task<ActionResult> ExportNotifications([FromQuery] NotificationFilterDto filter)
        {
            try
            {
                // Set a large page size for export
                filter.PageSize = 10000;
                filter.Page = 1;

                var result = await GetNotifications(filter);
                if (result.Result is OkObjectResult okResult && okResult.Value is PagedResult<NotificationDto> pagedResult)
                {
                    var csv = GenerateNotificationCsv(pagedResult.Data);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(csv);

                    return File(bytes, "text/csv", $"notifications_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
                }

                return StatusCode(500, new { error = "Failed to export notifications" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting notifications");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private string GenerateNotificationCsv(List<NotificationDto> notifications)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("ID,Type,Title,Message,Category,User,Tenant,Is Read,Is Global,Created At,Read At,Expires At");

            foreach (var notification in notifications)
            {
                csv.AppendLine($"{notification.Id}," +
                    $"{notification.Type}," +
                    $"\"{notification.Title?.Replace("\"", "\"\"")}\"," +
                    $"\"{notification.Message?.Replace("\"", "\"\"")}\"," +
                    $"{notification.Category}," +
                    $"{notification.UserId}," +
                    $"{notification.TenantId}," +
                    $"{notification.IsRead}," +
                    $"{notification.IsGlobal}," +
                    $"{notification.CreatedAt:yyyy-MM-dd HH:mm:ss}," +
                    $"{notification.ReadAt:yyyy-MM-dd HH:mm:ss}," +
                    $"{notification.ExpiresAt:yyyy-MM-dd HH:mm:ss}");
            }

            return csv.ToString();
        }
    }
}


