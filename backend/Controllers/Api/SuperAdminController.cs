using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using UmiHealthPOS.Models.Dashboard;
using UmiHealthPOS.Services;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using UmiHealthPOS.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace UmiHealthPOS.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "superadmin")]
    public class SuperAdminController : ControllerBase
    {
        private readonly ISuperAdminDashboardService _dashboardService;
        private readonly ILogger<SuperAdminController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public SuperAdminController(
            ISuperAdminDashboardService dashboardService,
            ILogger<SuperAdminController> logger,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _dashboardService = dashboardService;
            _logger = logger;
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("dashboard/stats")]
        public async Task<ActionResult<SuperAdminDashboardStats>> GetDashboardStats()
        {
            try
            {
                var stats = await _dashboardService.GetSuperAdminDashboardStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Super Admin dashboard statistics");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("dashboard/recent-activity")]
        public async Task<ActionResult<List<RecentActivity>>> GetRecentActivity([FromQuery] int limit = 10)
        {
            try
            {
                var activities = await _dashboardService.GetRecentActivityAsync(limit);
                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent activity");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("dashboard/revenue-chart")]
        public async Task<ActionResult<SuperAdminChartData>> GetRevenueChart([FromQuery] string period = "monthly")
        {
            try
            {
                var chartData = await _dashboardService.GetRevenueChartDataAsync(period);
                return Ok(chartData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving revenue chart data");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("dashboard/user-growth-chart")]
        public async Task<ActionResult<SuperAdminChartData>> GetUserGrowthChart([FromQuery] string period = "monthly")
        {
            try
            {
                var chartData = await _dashboardService.GetUserGrowthChartDataAsync(period);
                return Ok(chartData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user growth chart data");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("dashboard/tenant-stats")]
        public async Task<ActionResult<List<TenantStats>>> GetTenantStats()
        {
            try
            {
                var tenantStats = await _dashboardService.GetTenantStatsAsync();
                return Ok(tenantStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenant statistics");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("dashboard/system-health")]
        public async Task<ActionResult<SystemHealth>> GetSystemHealth()
        {
            try
            {
                var systemHealth = await _dashboardService.GetSystemHealthAsync();
                return Ok(systemHealth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving system health");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("dashboard/top-performers")]
        public async Task<ActionResult<List<TopPerformer>>> GetTopPerformers([FromQuery] string metric = "revenue", [FromQuery] int limit = 10)
        {
            try
            {
                var topPerformers = await _dashboardService.GetTopPerformersAsync(metric, limit);
                return Ok(topPerformers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving top performers");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        #region Tenant Impersonation

        [HttpGet("impersonation/stats")]
        public async Task<ActionResult<TenantImpersonationStats>> GetImpersonationStats()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var totalTenants = await _context.Tenants.CountAsync(t => t.IsActive);
                var activeSessions = await _context.TenantImpersonations
                    .CountAsync(i => i.Status == "Active" && i.EndTime == null);
                
                var today = DateTime.UtcNow.Date;
                var todaySessions = await _context.ImpersonationLogs
                    .CountAsync(l => l.AdminUserId == currentUserId && 
                                   l.Timestamp.Date == today && 
                                   l.Action == "Started");
                
                var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var monthlyLogs = await _context.ImpersonationLogs
                    .CountAsync(l => l.AdminUserId == currentUserId && 
                                   l.Timestamp >= thisMonth);

                return Ok(new TenantImpersonationStats
                {
                    TotalTenants = totalTenants,
                    ActiveSessions = activeSessions,
                    TodaySessions = todaySessions,
                    MonthlyLogs = monthlyLogs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving impersonation stats");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("impersonation/tenants")]
        public async Task<ActionResult<List<TenantImpersonationDto>>> GetTenantsForImpersonation(
            [FromQuery] string search = "", 
            [FromQuery] string status = "")
        {
            try
            {
                var query = _context.Tenants.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(t => 
                        t.PharmacyName.Contains(search) || 
                        t.AdminName.Contains(search) || 
                        t.Email.Contains(search));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(t => t.Status == status);
                }

                var tenants = await query
                    .Select(t => new TenantImpersonationDto
                    {
                        Id = t.Id,
                        TenantId = t.TenantId,
                        PharmacyName = t.PharmacyName,
                        AdminName = t.AdminName,
                        AdminEmail = t.Email,
                        PhoneNumber = t.PhoneNumber,
                        SubscriptionPlan = t.SubscriptionPlan,
                        Status = t.Status,
                        UserCount = _context.Users.Count(u => u.TenantId == t.TenantId && u.Status == "active"),
                        LastLogin = _context.Users
                            .Where(u => u.TenantId == t.TenantId && u.Status == "active")
                            .Max(u => (DateTime?)u.LastLoginAt),
                        CreatedAt = t.CreatedAt,
                        Logo = $"https://picsum.photos/seed/{t.TenantId}/40/40.jpg"
                    })
                    .OrderBy(t => t.PharmacyName)
                    .ToListAsync();

                return Ok(tenants);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenants for impersonation");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("impersonation/start")]
        public async Task<ActionResult<TenantImpersonationResponse>> StartImpersonation([FromBody] TenantImpersonationRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var tenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.Id == request.TenantId);

                if (tenant == null)
                {
                    return NotFound(new { error = "Tenant not found" });
                }

                if (tenant.Status != "Active")
                {
                    return BadRequest(new { error = "Cannot impersonate inactive tenant" });
                }

                // Check for existing active impersonation
                var existingImpersonation = await _context.TenantImpersonations
                    .FirstOrDefaultAsync(i => i.AdminUserId == currentUserId && 
                                           i.Status == "Active" && 
                                           i.EndTime == null);

                if (existingImpersonation != null)
                {
                    // End existing impersonation
                    existingImpersonation.EndTime = DateTime.UtcNow;
                    existingImpersonation.Status = "Ended";
                    
                    await _context.ImpersonationLogs.AddAsync(new ImpersonationLog
                    {
                        AdminUserId = currentUserId,
                        TenantId = existingImpersonation.TenantId,
                        TenantName = existingImpersonation.TenantName,
                        Action = "Stopped",
                        Timestamp = DateTime.UtcNow,
                        Duration = CalculateDuration(existingImpersonation.StartTime, DateTime.UtcNow),
                        IpAddress = GetClientIpAddress(),
                        UserAgent = Request.Headers["User-Agent"].ToString(),
                        Status = "Success"
                    });
                }

                // Create new impersonation session
                var impersonation = new TenantImpersonation
                {
                    AdminUserId = currentUserId,
                    TenantId = tenant.Id,
                    TenantName = tenant.PharmacyName,
                    StartTime = DateTime.UtcNow,
                    IpAddress = GetClientIpAddress(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    Status = "Active",
                    Notes = request.Reason
                };

                await _context.TenantImpersonations.AddAsync(impersonation);

                // Log the impersonation start
                await _context.ImpersonationLogs.AddAsync(new ImpersonationLog
                {
                    AdminUserId = currentUserId,
                    TenantId = tenant.Id,
                    TenantName = tenant.PharmacyName,
                    Action = "Started",
                    Timestamp = DateTime.UtcNow,
                    Duration = "Active",
                    IpAddress = GetClientIpAddress(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    Status = "Success"
                });

                await _context.SaveChangesAsync();

                // Generate impersonation token
                var impersonationToken = GenerateImpersonationToken(currentUserId, tenant.Id);

                return Ok(new TenantImpersonationResponse
                {
                    Success = true,
                    Message = $"Successfully started impersonation of {tenant.PharmacyName}",
                    ImpersonationToken = impersonationToken,
                    TenantDashboardUrl = $"/tenant/{tenant.TenantId}/dashboard",
                    StartTime = impersonation.StartTime
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting tenant impersonation");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("impersonation/stop")]
        public async Task<ActionResult> StopImpersonation()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var impersonation = await _context.TenantImpersonations
                    .FirstOrDefaultAsync(i => i.AdminUserId == currentUserId && 
                                           i.Status == "Active" && 
                                           i.EndTime == null);

                if (impersonation == null)
                {
                    return NotFound(new { error = "No active impersonation session found" });
                }

                // End the impersonation
                impersonation.EndTime = DateTime.UtcNow;
                impersonation.Status = "Ended";

                // Log the impersonation end
                await _context.ImpersonationLogs.AddAsync(new ImpersonationLog
                {
                    AdminUserId = currentUserId,
                    TenantId = impersonation.TenantId,
                    TenantName = impersonation.TenantName,
                    Action = "Stopped",
                    Timestamp = DateTime.UtcNow,
                    Duration = CalculateDuration(impersonation.StartTime, DateTime.UtcNow),
                    IpAddress = GetClientIpAddress(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    Status = "Success"
                });

                await _context.SaveChangesAsync();

                return Ok(new { message = "Impersonation session ended successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping tenant impersonation");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("impersonation/current")]
        public async Task<ActionResult<object>> GetCurrentImpersonation()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var impersonation = await _context.TenantImpersonations
                    .Include(i => i.Tenant)
                    .FirstOrDefaultAsync(i => i.AdminUserId == currentUserId && 
                                           i.Status == "Active" && 
                                           i.EndTime == null);

                if (impersonation == null)
                {
                    return Ok(new { currentImpersonation = (object?)null });
                }

                return Ok(new
                {
                    currentImpersonation = new
                    {
                        tenantId = impersonation.TenantId,
                        tenantName = impersonation.TenantName,
                        startTime = impersonation.StartTime,
                        duration = CalculateDuration(impersonation.StartTime, DateTime.UtcNow)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current impersonation");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("impersonation/logs")]
        public async Task<ActionResult<List<ImpersonationLog>>> GetImpersonationLogs(
            [FromQuery] int limit = 50,
            [FromQuery] string action = "",
            [FromQuery] string status = "")
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var query = _context.ImpersonationLogs
                    .Where(l => l.AdminUserId == currentUserId)
                    .OrderByDescending(l => l.Timestamp)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(action))
                {
                    query = query.Where(l => l.Action == action);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(l => l.Status == status);
                }

                var logs = await query
                    .Take(limit)
                    .Select(l => new
                    {
                        id = l.Id,
                        timestamp = l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                        admin = (l.AdminUser != null ? l.AdminUser.FirstName + " " + l.AdminUser.LastName : "Unknown"),
                        tenant = l.TenantName,
                        action = l.Action,
                        duration = l.Duration,
                        ipAddress = l.IpAddress,
                        status = l.Status
                    })
                    .ToListAsync();

                return Ok(logs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving impersonation logs");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        #endregion

        #region Category Management

        [HttpGet("categories")]
        public async Task<ActionResult<List<CategoryDto>>> GetCategories([FromQuery] string status = "", [FromQuery] string search = "")
        {
            try
            {
                var query = _context.Categories.AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(c => c.Status == status);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c => 
                        c.Name.ToLower().Contains(search.ToLower()) ||
                        c.Code.ToLower().Contains(search.ToLower()) ||
                        c.Description.ToLower().Contains(search.ToLower()));
                }

                var categories = await query
                    .OrderBy(c => c.Name)
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Code = c.Code,
                        Description = c.Description,
                        Status = c.Status,
                        ColorClass = c.ColorClass,
                        ItemCount = c.ItemCount,
                        TenantUsage = c.TenantUsage,
                        RequestedBy = c.RequestedBy,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        ApprovedAt = c.ApprovedAt,
                        ApprovedBy = c.ApprovedBy
                    })
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("categories/stats")]
        public async Task<ActionResult<CategoryStatsDto>> GetCategoryStats()
        {
            try
            {
                var totalCategories = await _context.Categories.CountAsync();
                var activeCategories = await _context.Categories.CountAsync(c => c.Status == "Active");
                var pendingCategories = await _context.Categories.CountAsync(c => c.Status == "Pending");
                var inactiveCategories = await _context.Categories.CountAsync(c => c.Status == "Inactive");
                var tenantUsage = await _context.TenantCategories.CountAsync(tc => tc.IsActive);

                return Ok(new CategoryStatsDto
                {
                    TotalCategories = totalCategories,
                    ActiveCategories = activeCategories,
                    PendingCategories = pendingCategories,
                    InactiveCategories = inactiveCategories,
                    TenantUsage = tenantUsage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category stats");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("categories")]
        public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Check if category code already exists
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Code.ToLower() == request.Code.ToLower());

                if (existingCategory != null)
                {
                    return BadRequest(new { error = "Category code already exists" });
                }

                var category = new Category
                {
                    Name = request.Name,
                    Code = request.Code,
                    Description = request.Description,
                    ColorClass = request.ColorClass,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Categories.AddAsync(category);
                await _context.SaveChangesAsync();

                var categoryDto = new CategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Code = category.Code,
                    Description = category.Description,
                    Status = category.Status,
                    ColorClass = category.ColorClass,
                    ItemCount = category.ItemCount,
                    TenantUsage = category.TenantUsage,
                    RequestedBy = category.RequestedBy,
                    CreatedAt = category.CreatedAt,
                    UpdatedAt = category.UpdatedAt,
                    ApprovedAt = category.ApprovedAt,
                    ApprovedBy = category.ApprovedBy
                };

                return CreatedAtAction(nameof(GetCategories), new { id = category.Id }, categoryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("categories/{id}")]
        public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return NotFound(new { error = "Category not found" });
                }

                // Check if category code already exists (excluding this category)
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Code.ToLower() == request.Code.ToLower() && c.Id != id);

                if (existingCategory != null)
                {
                    return BadRequest(new { error = "Category code already exists" });
                }

                category.Name = request.Name;
                category.Code = request.Code;
                category.Description = request.Description;
                category.ColorClass = request.ColorClass;
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var categoryDto = new CategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    Code = category.Code,
                    Description = category.Description,
                    Status = category.Status,
                    ColorClass = category.ColorClass,
                    ItemCount = category.ItemCount,
                    TenantUsage = category.TenantUsage,
                    RequestedBy = category.RequestedBy,
                    CreatedAt = category.CreatedAt,
                    UpdatedAt = category.UpdatedAt,
                    ApprovedAt = category.ApprovedAt,
                    ApprovedBy = category.ApprovedBy
                };

                return Ok(categoryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("categories/{id}")]
        public async Task<ActionResult> DeleteCategory(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return NotFound(new { error = "Category not found" });
                }

                // Check if category is being used by tenants
                var tenantUsage = await _context.TenantCategories
                    .CountAsync(tc => tc.CategoryId == id && tc.IsActive);

                if (tenantUsage > 0)
                {
                    return BadRequest(new { error = "Cannot delete category that is in use by tenants" });
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Category deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("categories/approve")]
        public async Task<ActionResult> ApproveCategory([FromBody] CategoryApprovalRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var category = await _context.Categories.FindAsync(request.CategoryId);
                if (category == null)
                {
                    return NotFound(new { error = "Category not found" });
                }

                if (request.Action == "Approve")
                {
                    category.Status = "Active";
                    category.ApprovedAt = DateTime.UtcNow;
                    category.ApprovedBy = currentUserId;
                    category.RequestedBy = null;
                    category.UpdatedAt = DateTime.UtcNow;
                }
                else if (request.Action == "Reject")
                {
                    _context.Categories.Remove(category);
                }
                else
                {
                    return BadRequest(new { error = "Invalid action" });
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = $"Category {request.Action.ToLower()}d successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving/rejecting category");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("categories/status")]
        public async Task<ActionResult> UpdateCategoryStatus([FromBody] CategoryStatusRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var category = await _context.Categories.FindAsync(request.CategoryId);
                if (category == null)
                {
                    return NotFound(new { error = "Category not found" });
                }

                if (request.Status != "Active" && request.Status != "Inactive")
                {
                    return BadRequest(new { error = "Invalid status" });
                }

                category.Status = request.Status;
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = $"Category status updated to {request.Status}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category status");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("categories/{id}/usage")]
        public async Task<ActionResult<CategoryUsageDto>> GetCategoryUsage(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.TenantCategories)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    return NotFound(new { error = "Category not found" });
                }

                var tenantUsages = await _context.TenantCategories
                    .Where(tc => tc.CategoryId == id && tc.IsActive)
                    .Join(_context.Tenants,
                        tc => tc.TenantId,
                        t => t.TenantId,
                        (tc, t) => new TenantUsageDetail
                        {
                            TenantId = tc.TenantId,
                            PharmacyName = t.PharmacyName,
                            UsageCount = tc.UsageCount,
                            LastUsedAt = tc.LastUsedAt ?? DateTime.MinValue
                        })
                    .ToListAsync();

                var usageDto = new CategoryUsageDto
                {
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    CategoryCode = category.Code,
                    TenantCount = tenantUsages.Count,
                    ItemCount = category.ItemCount,
                    TenantUsages = tenantUsages
                };

                return Ok(usageDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category usage");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("categories/sync")]
        public async Task<ActionResult> SyncCategoriesToTenants([FromBody] CategorySyncRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var categoriesToSync = request.CategoryIds.Any() 
                    ? await _context.Categories.Where(c => request.CategoryIds.Contains(c.Id) && c.Status == "Active").ToListAsync()
                    : await _context.Categories.Where(c => c.Status == "Active").ToListAsync();

                var tenantsToSync = request.TenantIds.Any()
                    ? await _context.Tenants.Where(t => request.TenantIds.Contains(t.TenantId) && t.Status == "Active").ToListAsync()
                    : await _context.Tenants.Where(t => t.Status == "Active").ToListAsync();

                var syncResults = new List<object>();

                foreach (var category in categoriesToSync)
                {
                    foreach (var tenant in tenantsToSync)
                    {
                        var existingTenantCategory = await _context.TenantCategories
                            .FirstOrDefaultAsync(tc => tc.CategoryId == category.Id && tc.TenantId == tenant.TenantId);

                        if (existingTenantCategory == null)
                        {
                            var tenantCategory = new TenantCategory
                            {
                                CategoryId = category.Id,
                                TenantId = tenant.TenantId,
                                IsActive = true,
                                AssignedAt = DateTime.UtcNow
                            };

                            await _context.TenantCategories.AddAsync(tenantCategory);

                            var categorySync = new CategorySync
                            {
                                CategoryId = category.Id,
                                TenantId = tenant.TenantId,
                                SyncStatus = "Synced",
                                SyncAt = DateTime.UtcNow
                            };

                            await _context.CategorySyncs.AddAsync(categorySync);

                            syncResults.Add(new { category = category.Name, tenant = tenant.PharmacyName, status = "Synced" });
                        }
                        else
                        {
                            syncResults.Add(new { category = category.Name, tenant = tenant.PharmacyName, status = "Already Exists" });
                        }
                    }

                    // Update tenant usage count
                    category.TenantUsage = await _context.TenantCategories.CountAsync(tc => tc.CategoryId == category.Id && tc.IsActive);
                }

                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Category synchronization completed",
                    syncedCategories = categoriesToSync.Count,
                    syncedTenants = tenantsToSync.Count,
                    results = syncResults
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing categories to tenants");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        #endregion

        #region Controlled Substances

        [HttpGet("controlled-substances")]
        public async Task<ActionResult<List<ControlledSubstanceDto>>> GetControlledSubstances()
        {
            try
            {
                var substances = await _context.ControlledSubstances
                    .Include(cs => cs.Tenant)
                    .OrderBy(cs => cs.Name)
                    .ToListAsync();

                var substanceDtos = substances.Select(cs => new ControlledSubstanceDto
                {
                    Id = cs.Id,
                    Name = cs.Name,
                    GenericName = cs.GenericName,
                    Schedule = cs.Schedule,
                    TenantId = cs.TenantId,
                    CurrentStock = cs.CurrentStock,
                    Unit = cs.Unit,
                    LastDispensed = cs.LastDispensed,
                    Status = cs.Status,
                    ComplianceScore = cs.ComplianceScore,
                    RegistrationNumber = cs.RegistrationNumber,
                    LastAudit = cs.LastAudit,
                    NextAuditDue = cs.NextAuditDue,
                    MonthlyDispensed = cs.MonthlyDispensed,
                    CreatedAt = cs.CreatedAt,
                    UpdatedAt = cs.UpdatedAt,
                    PharmacyName = cs.Tenant.PharmacyName,
                    PharmacyEmail = cs.Tenant.Email,
                    PharmacyLicense = cs.Tenant.LicenseNumber ?? "",
                    PharmacyPhone = cs.Tenant.PhoneNumber,
                    PharmacyAvatar = $"https://picsum.photos/seed/{cs.TenantId}/40/40.jpg"
                }).ToList();

                return Ok(substanceDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching controlled substances");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("controlled-substances/{id}")]
        public async Task<ActionResult<ControlledSubstanceDto>> GetControlledSubstance(int id)
        {
            try
            {
                var substance = await _context.ControlledSubstances
                    .Include(cs => cs.Tenant)
                    .FirstOrDefaultAsync(cs => cs.Id == id);

                if (substance == null)
                {
                    return NotFound(new { error = "Controlled substance not found" });
                }

                var substanceDto = new ControlledSubstanceDto
                {
                    Id = substance.Id,
                    Name = substance.Name,
                    GenericName = substance.GenericName,
                    Schedule = substance.Schedule,
                    TenantId = substance.TenantId,
                    CurrentStock = substance.CurrentStock,
                    Unit = substance.Unit,
                    LastDispensed = substance.LastDispensed,
                    Status = substance.Status,
                    ComplianceScore = substance.ComplianceScore,
                    RegistrationNumber = substance.RegistrationNumber,
                    LastAudit = substance.LastAudit ?? DateTime.MinValue,
                    NextAuditDue = substance.NextAuditDue ?? DateTime.MinValue,
                    MonthlyDispensed = substance.MonthlyDispensed,
                    CreatedAt = substance.CreatedAt,
                    UpdatedAt = substance.UpdatedAt,
                    PharmacyName = substance.Tenant.PharmacyName,
                    PharmacyEmail = substance.Tenant.Email,
                    PharmacyLicense = substance.Tenant.LicenseNumber ?? "",
                    PharmacyPhone = substance.Tenant.PhoneNumber,
                    PharmacyAvatar = $"https://picsum.photos/seed/{substance.TenantId}/40/40.jpg"
                };

                return Ok(substanceDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching controlled substance {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("controlled-substances")]
        public async Task<ActionResult<ControlledSubstanceDto>> CreateControlledSubstance([FromBody] CreateControlledSubstanceRequest request)
        {
            try
            {
                // Validate tenant exists
                var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.TenantId == request.TenantId);
                if (tenant == null)
                {
                    return BadRequest(new { error = "Invalid tenant ID" });
                }

                var substance = new ControlledSubstance
                {
                    Name = request.Name,
                    GenericName = request.GenericName,
                    Schedule = request.Schedule,
                    TenantId = request.TenantId,
                    CurrentStock = request.CurrentStock,
                    Unit = request.Unit,
                    RegistrationNumber = request.RegistrationNumber,
                    Status = "Compliant",
                    ComplianceScore = 100,
                    LastAudit = DateTime.UtcNow,
                    NextAuditDue = DateTime.UtcNow.AddDays(30),
                    MonthlyDispensed = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.ControlledSubstances.Add(substance);
                await _context.SaveChangesAsync();

                // Return the created substance with tenant details
                var createdSubstance = await _context.ControlledSubstances
                    .Include(cs => cs.Tenant)
                    .FirstOrDefaultAsync(cs => cs.Id == substance.Id);

                var substanceDto = new ControlledSubstanceDto
                {
                    Id = createdSubstance.Id,
                    Name = createdSubstance.Name,
                    GenericName = createdSubstance.GenericName,
                    Schedule = createdSubstance.Schedule,
                    TenantId = createdSubstance.TenantId,
                    CurrentStock = createdSubstance.CurrentStock,
                    Unit = createdSubstance.Unit,
                    LastDispensed = createdSubstance.LastDispensed,
                    Status = createdSubstance.Status,
                    ComplianceScore = createdSubstance.ComplianceScore,
                    RegistrationNumber = createdSubstance.RegistrationNumber,
                    LastAudit = createdSubstance.LastAudit,
                    NextAuditDue = createdSubstance.NextAuditDue,
                    MonthlyDispensed = createdSubstance.MonthlyDispensed,
                    CreatedAt = createdSubstance.CreatedAt,
                    UpdatedAt = createdSubstance.UpdatedAt,
                    PharmacyName = createdSubstance.Tenant.PharmacyName,
                    PharmacyEmail = createdSubstance.Tenant.Email,
                    PharmacyLicense = createdSubstance.Tenant.LicenseNumber ?? "",
                    PharmacyPhone = createdSubstance.Tenant.PhoneNumber,
                    PharmacyAvatar = $"https://picsum.photos/seed/{createdSubstance.TenantId}/40/40.jpg"
                };

                return CreatedAtAction(nameof(GetControlledSubstance), new { id = substance.Id }, substanceDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating controlled substance");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("controlled-substances/{id}")]
        public async Task<ActionResult<ControlledSubstanceDto>> UpdateControlledSubstance(int id, [FromBody] UpdateControlledSubstanceRequest request)
        {
            try
            {
                var substance = await _context.ControlledSubstances.FindAsync(id);
                if (substance == null)
                {
                    return NotFound(new { error = "Controlled substance not found" });
                }

                substance.Name = request.Name;
                substance.GenericName = request.GenericName;
                substance.Schedule = request.Schedule;
                substance.CurrentStock = request.CurrentStock;
                substance.Unit = request.Unit;
                substance.RegistrationNumber = request.RegistrationNumber;
                substance.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Return updated substance with tenant details
                var updatedSubstance = await _context.ControlledSubstances
                    .Include(cs => cs.Tenant)
                    .FirstOrDefaultAsync(cs => cs.Id == id);

                var substanceDto = new ControlledSubstanceDto
                {
                    Id = updatedSubstance.Id,
                    Name = updatedSubstance.Name,
                    GenericName = updatedSubstance.GenericName,
                    Schedule = updatedSubstance.Schedule,
                    TenantId = updatedSubstance.TenantId,
                    CurrentStock = updatedSubstance.CurrentStock,
                    Unit = updatedSubstance.Unit,
                    LastDispensed = updatedSubstance.LastDispensed,
                    Status = updatedSubstance.Status,
                    ComplianceScore = updatedSubstance.ComplianceScore,
                    RegistrationNumber = updatedSubstance.RegistrationNumber,
                    LastAudit = updatedSubstance.LastAudit,
                    NextAuditDue = updatedSubstance.NextAuditDue,
                    MonthlyDispensed = updatedSubstance.MonthlyDispensed,
                    CreatedAt = updatedSubstance.CreatedAt,
                    UpdatedAt = updatedSubstance.UpdatedAt,
                    PharmacyName = updatedSubstance.Tenant.PharmacyName,
                    PharmacyEmail = updatedSubstance.Tenant.Email,
                    PharmacyLicense = updatedSubstance.Tenant.LicenseNumber ?? "",
                    PharmacyPhone = updatedSubstance.Tenant.PhoneNumber,
                    PharmacyAvatar = $"https://picsum.photos/seed/{updatedSubstance.TenantId}/40/40.jpg"
                };

                return Ok(substanceDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating controlled substance {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("controlled-substances/{id}")]
        public async Task<ActionResult> DeleteControlledSubstance(int id)
        {
            try
            {
                var substance = await _context.ControlledSubstances.FindAsync(id);
                if (substance == null)
                {
                    return NotFound(new { error = "Controlled substance not found" });
                }

                _context.ControlledSubstances.Remove(substance);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Controlled substance deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting controlled substance {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("controlled-substances/{id}/audit")]
        public async Task<ActionResult<ControlledSubstanceAuditDto>> ConductAudit(int id, [FromBody] CreateAuditRequest request)
        {
            try
            {
                var substance = await _context.ControlledSubstances.FindAsync(id);
                if (substance == null)
                {
                    return NotFound(new { error = "Controlled substance not found" });
                }

                var audit = new ControlledSubstanceAudit
                {
                    SubstanceId = id,
                    AuditorName = "Super Admin", // In real app, get from current user
                    Finding = request.Finding,
                    Summary = request.Notes,
                    DiscrepancyDetails = request.DiscrepancyDetails,
                    RequiredAction = request.RequiredAction,
                    PreviousStock = substance.CurrentStock,
                    NewStock = request.CurrentStock,
                    AuditDate = DateTime.UtcNow
                };

                // Update substance based on audit findings
                substance.CurrentStock = request.CurrentStock;
                substance.LastAudit = DateTime.UtcNow;
                substance.Status = request.Finding == "violation" ? "Non-Compliant" : 
                                 request.Finding == "discrepancy" ? "Review Required" : "Compliant";
                substance.ComplianceScore = request.Finding == "violation" ? Math.Max(0, substance.ComplianceScore - 20) :
                                       request.Finding == "discrepancy" ? Math.Max(0, substance.ComplianceScore - 10) :
                                       Math.Min(100, substance.ComplianceScore + 5);
                
                // Set next audit date based on findings
                substance.NextAuditDue = request.Finding == "violation" ? DateTime.UtcNow.AddDays(7) :
                                        request.Finding == "discrepancy" ? DateTime.UtcNow.AddDays(14) :
                                        DateTime.UtcNow.AddDays(30);

                _context.ControlledSubstanceAudits.Add(audit);
                await _context.SaveChangesAsync();

                var auditDto = new ControlledSubstanceAuditDto
                {
                    Id = audit.Id,
                    SubstanceId = audit.SubstanceId,
                    AuditorName = audit.AuditorName,
                    Finding = audit.Finding,
                    Summary = audit.Summary,
                    DiscrepancyDetails = audit.DiscrepancyDetails,
                    RequiredAction = audit.RequiredAction,
                    PreviousStock = audit.PreviousStock,
                    NewStock = audit.NewStock,
                    AuditDate = audit.AuditDate,
                    Impact = audit.Finding == "compliant" ? "No impact on compliance score" :
                            audit.Finding == "discrepancy" ? "Minor impact - 10 point reduction" :
                            "Significant impact - 20 point reduction"
                };

                return Ok(auditDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error conducting audit for substance {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("controlled-substances/{id}/audit-history")]
        public async Task<ActionResult<List<ControlledSubstanceAuditDto>>> GetAuditHistory(int id)
        {
            try
            {
                var audits = await _context.ControlledSubstanceAudits
                    .Where(a => a.SubstanceId == id)
                    .OrderByDescending(a => a.AuditDate)
                    .ToListAsync();

                var auditDtos = audits.Select(a => new ControlledSubstanceAuditDto
                {
                    Id = a.Id,
                    SubstanceId = a.SubstanceId,
                    AuditorName = a.AuditorName,
                    Finding = a.Finding,
                    Summary = a.Summary,
                    DiscrepancyDetails = a.DiscrepancyDetails,
                    RequiredAction = a.RequiredAction,
                    PreviousStock = a.PreviousStock,
                    NewStock = a.NewStock,
                    AuditDate = a.AuditDate,
                    Impact = a.Finding == "compliant" ? "No impact on compliance score" :
                            a.Finding == "discrepancy" ? "Minor impact - 10 point reduction" :
                            "Significant impact - 20 point reduction"
                }).ToList();

                return Ok(auditDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching audit history for substance {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("controlled-substances/{id}/compliance-report")]
        public async Task<ActionResult<ComplianceReportData>> GenerateComplianceReport(int id)
        {
            try
            {
                var substance = await _context.ControlledSubstances
                    .Include(cs => cs.Tenant)
                    .Include(cs => cs.AuditHistory)
                    .FirstOrDefaultAsync(cs => cs.Id == id);

                if (substance == null)
                {
                    return NotFound(new { error = "Controlled substance not found" });
                }

                var auditHistory = await _context.ControlledSubstanceAudits
                    .Where(a => a.SubstanceId == id)
                    .OrderByDescending(a => a.AuditDate)
                    .ToListAsync();

                var auditDtos = auditHistory.Select(a => new ControlledSubstanceAuditDto
                {
                    Id = a.Id,
                    SubstanceId = a.SubstanceId,
                    AuditorName = a.AuditorName,
                    Finding = a.Finding,
                    Summary = a.Summary,
                    DiscrepancyDetails = a.DiscrepancyDetails,
                    RequiredAction = a.RequiredAction,
                    PreviousStock = a.PreviousStock,
                    NewStock = a.NewStock,
                    AuditDate = a.AuditDate,
                    Impact = a.Finding == "compliant" ? "No impact on compliance score" :
                            a.Finding == "discrepancy" ? "Minor impact - 10 point reduction" :
                            "Significant impact - 20 point reduction"
                }).ToList();

                // Generate recommendations
                var recommendations = GenerateRecommendations(substance);

                // Assess risk
                var riskAssessment = AssessRisk(substance);

                var reportData = new ComplianceReportData
                {
                    Substance = new ControlledSubstanceDto
                    {
                        Id = substance.Id,
                        Name = substance.Name,
                        GenericName = substance.GenericName,
                        Schedule = substance.Schedule,
                        TenantId = substance.TenantId,
                        CurrentStock = substance.CurrentStock,
                        Unit = substance.Unit,
                        LastDispensed = substance.LastDispensed ?? DateTime.MinValue,
                        Status = substance.Status,
                        ComplianceScore = (int)substance.ComplianceScore,
                        RegistrationNumber = substance.RegistrationNumber,
                        LastAudit = substance.LastAudit ?? DateTime.MinValue,
                        NextAuditDue = substance.NextAuditDue ?? DateTime.MinValue,
                        MonthlyDispensed = substance.MonthlyDispensed,
                        CreatedAt = substance.CreatedAt,
                        UpdatedAt = substance.UpdatedAt,
                        PharmacyName = substance.Tenant.PharmacyName,
                        PharmacyEmail = substance.Tenant.Email,
                        PharmacyLicense = substance.Tenant.LicenseNumber ?? "",
                        PharmacyPhone = substance.Tenant.PhoneNumber,
                        PharmacyAvatar = $"https://picsum.photos/seed/{substance.TenantId}/40/40.jpg"
                    },
                    ReportDate = DateTime.UtcNow.ToString("MMMM dd, yyyy"),
                    ReportId = $"RPT-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    GeneratedBy = "Super Admin",
                    ComplianceScore = (int)substance.ComplianceScore,
                    AuditHistory = auditDtos,
                    Recommendations = recommendations,
                    RiskAssessment = riskAssessment
                };

                return Ok(reportData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating compliance report for substance {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("tenants")]
        public async Task<ActionResult<List<TenantDto>>> GetTenants()
        {
            try
            {
                var tenants = await _context.Tenants
                    .OrderBy(t => t.PharmacyName)
                    .ToListAsync();

                var tenantDtos = tenants.Select(t => new TenantDto
                {
                    Id = t.Id,
                    TenantId = t.TenantId,
                    PharmacyName = t.PharmacyName,
                    AdminName = t.AdminName,
                    PhoneNumber = t.PhoneNumber,
                    Email = t.Email,
                    Address = t.Address,
                    LicenseNumber = t.LicenseNumber,
                    ZambiaRegNumber = t.ZambiaRegNumber,
                    SubscriptionPlan = t.SubscriptionPlan?.ToString() ?? "",
                    Status = t.Status,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    Avatar = $"https://picsum.photos/seed/{t.TenantId}/40/40.jpg"
                }).ToList();

                return Ok(tenantDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching tenants");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        #endregion

        #region Helper Methods

        private List<RecommendationDto> GenerateRecommendations(ControlledSubstance substance)
        {
            var recommendations = new List<RecommendationDto>();
            
            if (substance.ComplianceScore < 85)
            {
                recommendations.Add(new RecommendationDto
                {
                    Priority = "High",
                    Action = "Increase audit frequency to weekly",
                    Reason = "Low compliance score requires closer monitoring"
                });
            }
            
            if (substance.Status == "Review Required")
            {
                recommendations.Add(new RecommendationDto
                {
                    Priority = "Medium",
                    Action = "Conduct full inventory reconciliation",
                    Reason = "Discrepancies found in last audit"
                });
            }
            
            if (substance.Schedule == "Schedule II" || substance.Schedule == "Schedule I")
            {
                recommendations.Add(new RecommendationDto
                {
                    Priority = "High",
                    Action = "Review access controls and logging",
                    Reason = "High-schedule substance requires strict security"
                });
            }
            
            if (substance.CurrentStock < 20)
            {
                recommendations.Add(new RecommendationDto
                {
                    Priority = "Medium",
                    Action = "Monitor stock levels closely",
                    Reason = "Low stock may indicate diversion risk"
                });
            }
            
            if (recommendations.Count == 0)
            {
                recommendations.Add(new RecommendationDto
                {
                    Priority = "Low",
                    Action = "Continue routine monitoring",
                    Reason = "Substance shows good compliance"
                });
            }
            
            return recommendations;
        }

        private RiskAssessmentDto AssessRisk(ControlledSubstance substance)
        {
            var riskLevel = "Low";
            var riskFactors = new List<string>();
            
            // Check various risk factors
            if (substance.ComplianceScore < 70)
            {
                riskLevel = "High";
                riskFactors.Add("Low compliance score");
            }
            else if (substance.ComplianceScore < 85)
            {
                riskLevel = "Medium";
                riskFactors.Add("Moderate compliance score");
            }
            
            if (substance.Schedule == "Schedule I" || substance.Schedule == "Schedule II")
            {
                riskFactors.Add("High-schedule classification");
                if (riskLevel == "Low") riskLevel = "Medium";
            }
            
            if (substance.Status == "Non-Compliant")
            {
                riskLevel = "High";
                riskFactors.Add("Current non-compliance status");
            }
            
            if (substance.CurrentStock < 10)
            {
                riskFactors.Add("Critically low stock levels");
                if (riskLevel == "Low") riskLevel = "Medium";
            }
            
            var score = 100;
            
            // Deduct points for various factors
            if (substance.ComplianceScore < 70) score -= 30;
            else if (substance.ComplianceScore < 85) score -= 15;
            
            if (substance.Schedule == "Schedule I") score -= 20;
            else if (substance.Schedule == "Schedule II") score -= 15;
            else if (substance.Schedule == "Schedule III") score -= 10;
            
            if (substance.Status == "Non-Compliant") score -= 25;
            else if (substance.Status == "Review Required") score -= 10;
            
            return new RiskAssessmentDto
            {
                Level = riskLevel,
                Factors = riskFactors.Count > 0 ? riskFactors : new List<string> { "No significant risk factors identified" },
                Score = Math.Max(0, Math.Min(100, score))
            };
        }

        private string GetClientIpAddress()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = "Unknown";
            }
            return ipAddress;
        }

        private string CalculateDuration(DateTime startTime, DateTime endTime)
        {
            var duration = endTime - startTime;
            var hours = (int)duration.TotalHours;
            var minutes = duration.Minutes;

            if (hours > 0)
            {
                return $"{hours}h {minutes}min";
            }
            return $"{minutes}min";
        }

        private string GenerateImpersonationToken(string adminUserId, int tenantId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "DefaultSecretKey123456789");
            
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("adminUserId", adminUserId),
                    new Claim("tenantId", tenantId.ToString()),
                    new Claim("isImpersonation", "true"),
                    new Claim(ClaimTypes.Role, "tenantadmin")
                }),
                Expires = DateTime.UtcNow.AddHours(24), // Impersonation token valid for 24 hours
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        #endregion

        #region Subscription Plan Management

        [HttpGet("subscription-plans")]
        public async Task<ActionResult<List<SubscriptionPlanDto>>> GetSubscriptionPlans([FromQuery] bool includeInactive = false)
        {
            try
            {
                var query = _context.SubscriptionPlans.AsQueryable();

                if (!includeInactive)
                {
                    query = query.Where(p => p.IsActive);
                }

                var plans = await query
                    .Select(p => new SubscriptionPlanDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        MaxUsers = p.MaxUsers,
                        MaxBranches = p.MaxBranches,
                        MaxStorageGB = p.MaxStorageGB,
                        Features = p.Features,
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                        TenantCount = _context.Subscriptions.Count(s => s.PlanId == p.Id && s.IsActive)
                    })
                    .OrderBy(p => p.Price)
                    .ToListAsync();

                return Ok(plans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription plans");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("subscription-plans/stats")]
        public async Task<ActionResult<SubscriptionPlanStatsDto>> GetSubscriptionPlanStats()
        {
            try
            {
                var totalPlans = await _context.SubscriptionPlans.CountAsync();
                var activePlans = await _context.SubscriptionPlans.CountAsync(p => p.IsActive);
                var inactivePlans = totalPlans - activePlans;
                var totalTenants = await _context.Subscriptions.CountAsync(s => s.IsActive);
                var totalRevenue = await _context.Subscriptions
                    .Where(s => s.IsActive)
                    .SumAsync(s => s.Amount);

                return Ok(new SubscriptionPlanStatsDto
                {
                    TotalPlans = totalPlans,
                    ActivePlans = activePlans,
                    InactivePlans = inactivePlans,
                    TotalRevenue = totalRevenue,
                    TotalTenants = totalTenants
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription plan stats");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("subscription-plans")]
        public async Task<ActionResult<SubscriptionPlanDto>> CreateSubscriptionPlan([FromBody] UmiHealthPOS.DTOs.CreateSubscriptionPlanRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Check if plan name already exists
                var existingPlan = await _context.SubscriptionPlans
                    .FirstOrDefaultAsync(p => p.Name.ToLower() == request.Name.ToLower());

                if (existingPlan != null)
                {
                    return BadRequest(new { error = "Subscription plan name already exists" });
                }

                var plan = new SubscriptionPlan
                {
                    Name = request.Name,
                    Price = request.Price,
                    MaxUsers = request.MaxUsers,
                    MaxBranches = request.MaxBranches,
                    MaxStorageGB = request.MaxStorageGB,
                    Features = request.Features,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.SubscriptionPlans.AddAsync(plan);
                await _context.SaveChangesAsync();

                var planDto = new SubscriptionPlanDto
                {
                    Id = plan.Id,
                    Name = plan.Name,
                    Price = plan.Price,
                    MaxUsers = plan.MaxUsers,
                    MaxBranches = plan.MaxBranches,
                    MaxStorageGB = plan.MaxStorageGB,
                    Features = plan.Features,
                    IsActive = plan.IsActive,
                    CreatedAt = plan.CreatedAt,
                    UpdatedAt = plan.UpdatedAt,
                    TenantCount = 0
                };

                return CreatedAtAction(nameof(GetSubscriptionPlans), new { id = plan.Id }, planDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription plan");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("subscription-plans/{id}")]
        public async Task<ActionResult<SubscriptionPlanDto>> UpdateSubscriptionPlan(int id, [FromBody] UmiHealthPOS.DTOs.UpdateSubscriptionPlanRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var plan = await _context.SubscriptionPlans.FindAsync(id);
                if (plan == null)
                {
                    return NotFound(new { error = "Subscription plan not found" });
                }

                // Check if plan name already exists (excluding this plan)
                var existingPlan = await _context.SubscriptionPlans
                    .FirstOrDefaultAsync(p => p.Name.ToLower() == request.Name.ToLower() && p.Id != id);

                if (existingPlan != null)
                {
                    return BadRequest(new { error = "Subscription plan name already exists" });
                }

                plan.Name = request.Name;
                plan.Price = request.Price;
                plan.MaxUsers = request.MaxUsers;
                plan.MaxBranches = request.MaxBranches;
                plan.MaxStorageGB = request.MaxStorageGB;
                plan.Features = request.Features;
                plan.IsActive = request.IsActive;
                plan.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var planDto = new SubscriptionPlanDto
                {
                    Id = plan.Id,
                    Name = plan.Name,
                    Price = plan.Price,
                    MaxUsers = plan.MaxUsers,
                    MaxBranches = plan.MaxBranches,
                    MaxStorageGB = plan.MaxStorageGB,
                    Features = plan.Features,
                    IsActive = plan.IsActive,
                    CreatedAt = plan.CreatedAt,
                    UpdatedAt = plan.UpdatedAt,
                    TenantCount = _context.Subscriptions.Count(s => s.PlanId == plan.Id && s.IsActive)
                };

                return Ok(planDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription plan");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("subscription-plans/{id}")]
        public async Task<ActionResult> DeleteSubscriptionPlan(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var plan = await _context.SubscriptionPlans.FindAsync(id);
                if (plan == null)
                {
                    return NotFound(new { error = "Subscription plan not found" });
                }

                // Check if plan is being used by tenants
                var tenantUsage = await _context.Subscriptions
                    .CountAsync(s => s.PlanId == id && s.IsActive);

                if (tenantUsage > 0)
                {
                    return BadRequest(new { error = "Cannot delete subscription plan that is in use by tenants" });
                }

                _context.SubscriptionPlans.Remove(plan);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Subscription plan deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subscription plan");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("subscription-plans/{id}/status")]
        public async Task<ActionResult> UpdateSubscriptionPlanStatus(int id, [FromBody] UmiHealthPOS.DTOs.UpdateStatusRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var plan = await _context.SubscriptionPlans.FindAsync(id);
                if (plan == null)
                {
                    return NotFound(new { error = "Subscription plan not found" });
                }

                if (request.Status != "Active" && request.Status != "Inactive")
                {
                    return BadRequest(new { error = "Invalid status" });
                }

                plan.IsActive = request.Status == "Active";
                plan.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = $"Subscription plan status updated to {request.Status}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription plan status");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        #endregion

        #region Application Feature Management

        [HttpGet("application-features")]
        public async Task<ActionResult<List<ApplicationFeatureDto>>> GetApplicationFeatures([FromQuery] bool includeInactive = false)
        {
            try
            {
                var query = _context.ApplicationFeatures.AsQueryable();

                if (!includeInactive)
                {
                    query = query.Where(f => f.IsActive);
                }

                var features = await query
                    .OrderBy(f => f.SortOrder)
                    .ThenBy(f => f.Name)
                    .Select(f => new ApplicationFeatureDto
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Description = f.Description,
                        Category = f.Category,
                        Module = f.Module,
                        BasicPlan = f.BasicPlan,
                        ProfessionalPlan = f.ProfessionalPlan,
                        EnterprisePlan = f.EnterprisePlan,
                        IsActive = f.IsActive,
                        SortOrder = f.SortOrder,
                        CreatedAt = f.CreatedAt,
                        UpdatedAt = f.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(features);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application features");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("application-features/stats")]
        public async Task<ActionResult<ApplicationFeatureStatsDto>> GetApplicationFeatureStats()
        {
            try
            {
                var totalFeatures = await _context.ApplicationFeatures.CountAsync();
                var activeFeatures = await _context.ApplicationFeatures.CountAsync(f => f.IsActive);
                var inactiveFeatures = totalFeatures - activeFeatures;
                var basicPlanFeatures = await _context.ApplicationFeatures.CountAsync(f => f.BasicPlan);
                var professionalPlanFeatures = await _context.ApplicationFeatures.CountAsync(f => f.ProfessionalPlan);
                var enterprisePlanFeatures = await _context.ApplicationFeatures.CountAsync(f => f.EnterprisePlan);

                return Ok(new ApplicationFeatureStatsDto
                {
                    TotalFeatures = totalFeatures,
                    ActiveFeatures = activeFeatures,
                    InactiveFeatures = inactiveFeatures,
                    BasicPlanFeatures = basicPlanFeatures,
                    ProfessionalPlanFeatures = professionalPlanFeatures,
                    EnterprisePlanFeatures = enterprisePlanFeatures
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application feature stats");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("application-features")]
        public async Task<ActionResult<ApplicationFeatureDto>> CreateApplicationFeature([FromBody] CreateApplicationFeatureRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Check if feature name already exists
                var existingFeature = await _context.ApplicationFeatures
                    .FirstOrDefaultAsync(f => f.Name.ToLower() == request.Name.ToLower());

                if (existingFeature != null)
                {
                    return BadRequest(new { error = "Application feature name already exists" });
                }

                var feature = new ApplicationFeature
                {
                    Name = request.Name,
                    Description = request.Description,
                    Category = request.Category,
                    Module = request.Module,
                    BasicPlan = request.BasicPlan,
                    ProfessionalPlan = request.ProfessionalPlan,
                    EnterprisePlan = request.EnterprisePlan,
                    IsActive = true,
                    SortOrder = request.SortOrder,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.ApplicationFeatures.AddAsync(feature);
                await _context.SaveChangesAsync();

                var featureDto = new ApplicationFeatureDto
                {
                    Id = feature.Id,
                    Name = feature.Name,
                    Description = feature.Description,
                    Category = feature.Category,
                    Module = feature.Module,
                    BasicPlan = feature.BasicPlan,
                    ProfessionalPlan = feature.ProfessionalPlan,
                    EnterprisePlan = feature.EnterprisePlan,
                    IsActive = feature.IsActive,
                    SortOrder = feature.SortOrder,
                    CreatedAt = feature.CreatedAt,
                    UpdatedAt = feature.UpdatedAt
                };

                return CreatedAtAction(nameof(GetApplicationFeatures), new { id = feature.Id }, featureDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating application feature");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("application-features/{id}")]
        public async Task<ActionResult<ApplicationFeatureDto>> UpdateApplicationFeature(int id, [FromBody] UpdateApplicationFeatureRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var feature = await _context.ApplicationFeatures.FindAsync(id);
                if (feature == null)
                {
                    return NotFound(new { error = "Application feature not found" });
                }

                // Check if feature name already exists (excluding this feature)
                var existingFeature = await _context.ApplicationFeatures
                    .FirstOrDefaultAsync(f => f.Name.ToLower() == request.Name.ToLower() && f.Id != id);

                if (existingFeature != null)
                {
                    return BadRequest(new { error = "Application feature name already exists" });
                }

                feature.Name = request.Name;
                feature.Description = request.Description;
                feature.Category = request.Category;
                feature.Module = request.Module;
                feature.BasicPlan = request.BasicPlan;
                feature.ProfessionalPlan = request.ProfessionalPlan;
                feature.EnterprisePlan = request.EnterprisePlan;
                feature.IsActive = request.IsActive;
                feature.SortOrder = request.SortOrder;
                feature.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var featureDto = new ApplicationFeatureDto
                {
                    Id = feature.Id,
                    Name = feature.Name,
                    Description = feature.Description,
                    Category = feature.Category,
                    Module = feature.Module,
                    BasicPlan = feature.BasicPlan,
                    ProfessionalPlan = feature.ProfessionalPlan,
                    EnterprisePlan = feature.EnterprisePlan,
                    IsActive = feature.IsActive,
                    SortOrder = feature.SortOrder,
                    CreatedAt = feature.CreatedAt,
                    UpdatedAt = feature.UpdatedAt
                };

                return Ok(featureDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application feature");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("application-features/{id}")]
        public async Task<ActionResult> DeleteApplicationFeature(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var feature = await _context.ApplicationFeatures.FindAsync(id);
                if (feature == null)
                {
                    return NotFound(new { error = "Application feature not found" });
                }

                // Check if feature is being used by subscription plans
                var planUsage = await _context.SubscriptionPlanFeatures
                    .CountAsync(pf => pf.ApplicationFeatureId == id && pf.IsEnabled);

                if (planUsage > 0)
                {
                    return BadRequest(new { error = "Cannot delete application feature that is in use by subscription plans" });
                }

                _context.ApplicationFeatures.Remove(feature);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Application feature deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting application feature");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("application-features/{id}/status")]
        public async Task<ActionResult> UpdateApplicationFeatureStatus(int id, [FromBody] UmiHealthPOS.DTOs.UpdateStatusRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var feature = await _context.ApplicationFeatures.FindAsync(id);
                if (feature == null)
                {
                    return NotFound(new { error = "Application feature not found" });
                }

                if (request.Status != "Active" && request.Status != "Inactive")
                {
                    return BadRequest(new { error = "Invalid status" });
                }

                feature.IsActive = request.Status == "Active";
                feature.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = $"Application feature status updated to {request.Status}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application feature status");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("application-features/{id}/plan-assignment")]
        public async Task<ActionResult> UpdateFeaturePlanAssignment(int id, [FromBody] UpdateFeaturePlanRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var feature = await _context.ApplicationFeatures.FindAsync(id);
                if (feature == null)
                {
                    return NotFound(new { error = "Application feature not found" });
                }

                feature.BasicPlan = request.BasicPlan;
                feature.ProfessionalPlan = request.ProfessionalPlan;
                feature.EnterprisePlan = request.EnterprisePlan;
                feature.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Feature plan assignment updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating feature plan assignment");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        #endregion

        #region RBAC Management

        [HttpGet("rbac/stats")]
        public async Task<ActionResult<RbacStatsDto>> GetRbacStats()
        {
            try
            {
                var totalRoles = await _context.Roles.CountAsync();
                var activeRoles = await _context.Roles.CountAsync(r => r.Status == "Active");
                var inactiveRoles = await _context.Roles.CountAsync(r => r.Status == "Inactive");
                var systemRoles = await _context.Roles.CountAsync(r => r.IsSystem);
                
                var totalPermissions = await _context.Permissions.CountAsync();
                var activePermissions = await _context.Permissions.CountAsync(p => p.Status == "Active");
                var criticalPermissions = await _context.Permissions.CountAsync(p => p.RiskLevel == "Critical");
                
                var totalRoleAssignments = await _context.UserRoles.CountAsync();
                var activeRoleAssignments = await _context.UserRoles.CountAsync(ur => ur.IsActive);
                var tenantRoleAssignments = await _context.TenantRoles.CountAsync();

                return Ok(new RbacStatsDto
                {
                    TotalRoles = totalRoles,
                    ActiveRoles = activeRoles,
                    InactiveRoles = inactiveRoles,
                    SystemRoles = systemRoles,
                    TotalPermissions = totalPermissions,
                    ActivePermissions = activePermissions,
                    CriticalPermissions = criticalPermissions,
                    TotalRoleAssignments = totalRoleAssignments,
                    ActiveRoleAssignments = activeRoleAssignments,
                    TenantRoleAssignments = tenantRoleAssignments
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving RBAC stats");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        #region Roles Management

        [HttpGet("rbac/roles")]
        public async Task<ActionResult<List<RoleDto>>> GetRoles([FromQuery] string status = "", [FromQuery] string level = "")
        {
            try
            {
                var query = _context.Roles.AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(r => r.Status == status);
                }

                if (!string.IsNullOrEmpty(level))
                {
                    query = query.Where(r => r.Level == level);
                }

                var roles = await query
                    .OrderBy(r => r.Name)
                    .Select(r => new RoleDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Description = r.Description,
                        Level = r.Level,
                        Status = r.Status,
                        IsSystem = r.IsSystem,
                        IsGlobal = r.IsGlobal,
                        PermissionCount = _context.RolePermissions.Count(rp => rp.RoleId == r.Id),
                        UserCount = _context.UserRoles.Count(ur => ur.RoleId == r.Id && ur.IsActive),
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("rbac/roles")]
        public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Check if role name already exists
                var existingRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name.ToLower() == request.Name.ToLower());

                if (existingRole != null)
                {
                    return BadRequest(new { error = "Role name already exists" });
                }

                var role = new Role
                {
                    Name = request.Name,
                    Description = request.Description,
                    Level = request.Level,
                    Status = "Active",
                    IsGlobal = request.IsGlobal,
                    IsSystem = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Roles.AddAsync(role);
                await _context.SaveChangesAsync();

                // Assign permissions if provided
                if (request.PermissionIds.Length > 0)
                {
                    var rolePermissions = request.PermissionIds.Select(permissionId => new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permissionId,
                        CreatedAt = DateTime.UtcNow
                    });

                    await _context.RolePermissions.AddRangeAsync(rolePermissions);
                    await _context.SaveChangesAsync();
                }

                var roleDto = new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    Level = role.Level,
                    Status = role.Status,
                    IsSystem = role.IsSystem,
                    IsGlobal = role.IsGlobal,
                    PermissionCount = request.PermissionIds.Length,
                    UserCount = 0,
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt
                };

                return CreatedAtAction(nameof(GetRoles), new { id = role.Id }, roleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("rbac/roles/{id}")]
        public async Task<ActionResult<RoleDto>> UpdateRole(int id, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                {
                    return NotFound(new { error = "Role not found" });
                }

                if (role.IsSystem)
                {
                    return BadRequest(new { error = "Cannot modify system roles" });
                }

                // Check if role name already exists (excluding this role)
                var existingRole = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name.ToLower() == request.Name.ToLower() && r.Id != id);

                if (existingRole != null)
                {
                    return BadRequest(new { error = "Role name already exists" });
                }

                role.Name = request.Name;
                role.Description = request.Description;
                role.Level = request.Level;
                role.IsGlobal = request.IsGlobal;
                role.UpdatedAt = DateTime.UtcNow;

                // Update role permissions
                var existingPermissions = await _context.RolePermissions
                    .Where(rp => rp.RoleId == id)
                    .ToListAsync();

                _context.RolePermissions.RemoveRange(existingPermissions);

                if (request.PermissionIds.Length > 0)
                {
                    var rolePermissions = request.PermissionIds.Select(permissionId => new RolePermission
                    {
                        RoleId = id,
                        PermissionId = permissionId,
                        CreatedAt = DateTime.UtcNow
                    });

                    await _context.RolePermissions.AddRangeAsync(rolePermissions);
                }

                await _context.SaveChangesAsync();

                var roleDto = new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    Level = role.Level,
                    Status = role.Status,
                    IsSystem = role.IsSystem,
                    IsGlobal = role.IsGlobal,
                    PermissionCount = request.PermissionIds.Length,
                    UserCount = _context.UserRoles.Count(ur => ur.RoleId == id && ur.IsActive),
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt
                };

                return Ok(roleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("rbac/roles/{id}")]
        public async Task<ActionResult> DeleteRole(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                {
                    return NotFound(new { error = "Role not found" });
                }

                if (role.IsSystem)
                {
                    return BadRequest(new { error = "Cannot delete system roles" });
                }

                // Check if role is being used
                var userCount = await _context.UserRoles.CountAsync(ur => ur.RoleId == id && ur.IsActive);
                var tenantCount = await _context.TenantRoles.CountAsync(tr => tr.RoleId == id && tr.IsActive);

                if (userCount > 0 || tenantCount > 0)
                {
                    return BadRequest(new { error = "Cannot delete role that is in use" });
                }

                // Remove role permissions
                var rolePermissions = await _context.RolePermissions
                    .Where(rp => rp.RoleId == id)
                    .ToListAsync();

                _context.RolePermissions.RemoveRange(rolePermissions);
                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Role deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("rbac/roles/{id}/status")]
        public async Task<ActionResult> UpdateRoleStatus(int id, [FromBody] RoleStatusRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                {
                    return NotFound(new { error = "Role not found" });
                }

                if (role.IsSystem)
                {
                    return BadRequest(new { error = "Cannot modify system roles" });
                }

                if (request.Status != "Active" && request.Status != "Inactive")
                {
                    return BadRequest(new { error = "Invalid status" });
                }

                role.Status = request.Status;
                role.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = $"Role status updated to {request.Status}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role status");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("rbac/roles/{id}/permissions")]
        public async Task<ActionResult<List<RolePermissionDto>>> GetRolePermissions(int id)
        {
            try
            {
                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                {
                    return NotFound(new { error = "Role not found" });
                }

                var permissions = await _context.RolePermissions
                    .Where(rp => rp.RoleId == id)
                    .Include(rp => rp.Permission)
                    .Select(rp => new RolePermissionDto
                    {
                        RoleId = rp.RoleId,
                        RoleName = role.Name,
                        PermissionId = rp.PermissionId,
                        PermissionName = rp.Permission.Name,
                        PermissionDisplayName = rp.Permission.DisplayName,
                        Category = rp.Permission.Category,
                        RiskLevel = rp.Permission.RiskLevel,
                        AssignedAt = rp.CreatedAt
                    })
                    .OrderBy(rp => rp.Category)
                    .ThenBy(rp => rp.PermissionName)
                    .ToListAsync();

                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving role permissions");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        #endregion

        #region Permissions Management

        [HttpGet("rbac/permissions")]
        public async Task<ActionResult<List<PermissionDto>>> GetPermissions([FromQuery] string status = "", [FromQuery] string category = "", [FromQuery] string riskLevel = "")
        {
            try
            {
                var query = _context.Permissions.AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status);
                }

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(p => p.Category == category);
                }

                if (!string.IsNullOrEmpty(riskLevel))
                {
                    query = query.Where(p => p.RiskLevel == riskLevel);
                }

                var permissions = await query
                    .OrderBy(p => p.Category)
                    .ThenBy(p => p.Name)
                    .Select(p => new PermissionDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        DisplayName = p.DisplayName,
                        Category = p.Category,
                        Description = p.Description,
                        RiskLevel = p.RiskLevel,
                        Status = p.Status,
                        IsSystem = p.IsSystem,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                        RoleCount = _context.RolePermissions.Count(rp => rp.PermissionId == p.Id)
                    })
                    .ToListAsync();

                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving permissions");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("rbac/permissions")]
        public async Task<ActionResult<PermissionDto>> CreatePermission([FromBody] CreatePermissionRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Check if permission name already exists
                var existingPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name.ToLower() == request.Name.ToLower());

                if (existingPermission != null)
                {
                    return BadRequest(new { error = "Permission name already exists" });
                }

                var permission = new Permission
                {
                    Name = request.Name,
                    DisplayName = request.DisplayName,
                    Category = request.Category,
                    Description = request.Description,
                    RiskLevel = request.RiskLevel,
                    Status = "Active",
                    IsSystem = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.Permissions.AddAsync(permission);
                await _context.SaveChangesAsync();

                var permissionDto = new PermissionDto
                {
                    Id = permission.Id,
                    Name = permission.Name,
                    DisplayName = permission.DisplayName,
                    Category = permission.Category,
                    Description = permission.Description,
                    RiskLevel = permission.RiskLevel,
                    Status = permission.Status,
                    IsSystem = permission.IsSystem,
                    CreatedAt = permission.CreatedAt,
                    UpdatedAt = permission.UpdatedAt,
                    RoleCount = 0
                };

                return CreatedAtAction(nameof(GetPermissions), new { id = permission.Id }, permissionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating permission");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("rbac/permissions/{id}")]
        public async Task<ActionResult<PermissionDto>> UpdatePermission(int id, [FromBody] UpdatePermissionRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var permission = await _context.Permissions.FindAsync(id);
                if (permission == null)
                {
                    return NotFound(new { error = "Permission not found" });
                }

                if (permission.IsSystem)
                {
                    return BadRequest(new { error = "Cannot modify system permissions" });
                }

                // Check if permission name already exists (excluding this permission)
                var existingPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name.ToLower() == request.Name.ToLower() && p.Id != id);

                if (existingPermission != null)
                {
                    return BadRequest(new { error = "Permission name already exists" });
                }

                permission.Name = request.Name;
                permission.DisplayName = request.DisplayName;
                permission.Category = request.Category;
                permission.Description = request.Description;
                permission.RiskLevel = request.RiskLevel;
                permission.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var permissionDto = new PermissionDto
                {
                    Id = permission.Id,
                    Name = permission.Name,
                    DisplayName = permission.DisplayName,
                    Category = permission.Category,
                    Description = permission.Description,
                    RiskLevel = permission.RiskLevel,
                    Status = permission.Status,
                    IsSystem = permission.IsSystem,
                    CreatedAt = permission.CreatedAt,
                    UpdatedAt = permission.UpdatedAt,
                    RoleCount = _context.RolePermissions.Count(rp => rp.PermissionId == id)
                };

                return Ok(permissionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permission");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("rbac/permissions/{id}")]
        public async Task<ActionResult> DeletePermission(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var permission = await _context.Permissions.FindAsync(id);
                if (permission == null)
                {
                    return NotFound(new { error = "Permission not found" });
                }

                if (permission.IsSystem)
                {
                    return BadRequest(new { error = "Cannot delete system permissions" });
                }

                // Check if permission is being used
                var roleCount = await _context.RolePermissions.CountAsync(rp => rp.PermissionId == id);

                if (roleCount > 0)
                {
                    return BadRequest(new { error = "Cannot delete permission that is assigned to roles" });
                }

                _context.Permissions.Remove(permission);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Permission deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting permission");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("rbac/permissions/{id}/status")]
        public async Task<ActionResult> UpdatePermissionStatus(int id, [FromBody] PermissionStatusRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var permission = await _context.Permissions.FindAsync(id);
                if (permission == null)
                {
                    return NotFound(new { error = "Permission not found" });
                }

                if (permission.IsSystem)
                {
                    return BadRequest(new { error = "Cannot modify system permissions" });
                }

                if (request.Status != "Active" && request.Status != "Inactive")
                {
                    return BadRequest(new { error = "Invalid status" });
                }

                permission.Status = request.Status;
                permission.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = $"Permission status updated to {request.Status}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permission status");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        #endregion

        #region Tenant Role Assignments

        [HttpGet("rbac/tenant-assignments")]
        public async Task<ActionResult<List<TenantRoleAssignmentDto>>> GetTenantAssignments([FromQuery] string tenantId = "")
        {
            try
            {
                var query = _context.Tenants.AsQueryable();

                if (!string.IsNullOrEmpty(tenantId))
                {
                    query = query.Where(t => t.TenantId == tenantId);
                }

                var assignments = await query
                    .Select(t => new TenantRoleAssignmentDto
                    {
                        Id = t.Id,
                        Tenant = t.PharmacyName,
                        UserCount = _context.Users.Count(u => u.TenantId == t.TenantId && u.IsActive),
                        RoleCount = _context.TenantRoles.Count(tr => tr.TenantId == t.TenantId && tr.IsActive),
                        Status = t.Status,
                        LastActivity = _context.Users
                            .Where(u => u.TenantId == t.TenantId && u.IsActive)
                            .Max(u => u.LastLoginAt) ?? DateTime.MinValue
                    })
                    .OrderBy(a => a.Tenant)
                    .ToListAsync();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenant assignments");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("rbac/tenant-assignments/{tenantId}/roles")]
        public async Task<ActionResult<List<TenantRoleDto>>> GetTenantRoles(string tenantId)
        {
            try
            {
                var tenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.TenantId == tenantId);

                if (tenant == null)
                {
                    return NotFound(new { error = "Tenant not found" });
                }

                var roles = await _context.TenantRoles
                    .Where(tr => tr.TenantId == tenantId)
                    .Include(tr => tr.Role)
                    .Select(tr => new TenantRoleDto
                    {
                        Id = tr.Id,
                        TenantId = tr.TenantId,
                        TenantName = tenant.PharmacyName,
                        RoleId = tr.RoleId,
                        RoleName = tr.Role.Name,
                        RoleLevel = tr.Role.Level,
                        IsActive = tr.IsActive,
                        UserCount = _context.UserRoles.Count(ur => ur.RoleId == tr.RoleId && ur.TenantId == tenantId && ur.IsActive),
                        AssignedAt = tr.AssignedAt,
                        LastUsedAt = tr.LastUsedAt
                    })
                    .OrderBy(tr => tr.RoleName)
                    .ToListAsync();

                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tenant roles");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("rbac/tenant-assignments")]
        public async Task<ActionResult<TenantRoleDto>> AssignTenantRole([FromBody] AssignTenantRoleRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var tenant = await _context.Tenants
                    .FirstOrDefaultAsync(t => t.TenantId == request.TenantId);

                if (tenant == null)
                {
                    return NotFound(new { error = "Tenant not found" });
                }

                var role = await _context.Roles.FindAsync(request.RoleId);
                if (role == null)
                {
                    return NotFound(new { error = "Role not found" });
                }

                if (!role.IsGlobal)
                {
                    return BadRequest(new { error = "Cannot assign non-global role to tenant" });
                }

                // Check if assignment already exists
                var existingAssignment = await _context.TenantRoles
                    .FirstOrDefaultAsync(tr => tr.TenantId == request.TenantId && tr.RoleId == request.RoleId);

                if (existingAssignment != null)
                {
                    if (!existingAssignment.IsActive)
                    {
                        existingAssignment.IsActive = true;
                        existingAssignment.AssignedAt = DateTime.UtcNow;
                        existingAssignment.AssignedBy = currentUserId;
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        return BadRequest(new { error = "Role already assigned to tenant" });
                    }
                }
                else
                {
                    var tenantRole = new TenantRole
                    {
                        TenantId = request.TenantId,
                        RoleId = request.RoleId,
                        IsActive = true,
                        AssignedAt = DateTime.UtcNow,
                        AssignedBy = currentUserId
                    };

                    await _context.TenantRoles.AddAsync(tenantRole);
                    await _context.SaveChangesAsync();
                }

                var tenantRoleDto = new TenantRoleDto
                {
                    TenantId = request.TenantId,
                    TenantName = tenant.PharmacyName,
                    RoleId = request.RoleId,
                    RoleName = role.Name,
                    RoleLevel = role.Level,
                    IsActive = true,
                    UserCount = 0,
                    AssignedAt = DateTime.UtcNow
                };

                return Ok(tenantRoleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning tenant role");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        #endregion

        #region Notifications

        [HttpGet("notifications")]
        public async Task<ActionResult<object>> GetNotifications([FromQuery] NotificationFilterDto filter)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var notificationService = new NotificationService(_context, 
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationService>.Instance);
                
                var notifications = await notificationService.GetNotificationsAsync(filter, currentUserId);
                var stats = await notificationService.GetNotificationStatsAsync(currentUserId);

                return Ok(new { notifications, stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("notifications/{id}")]
        public async Task<ActionResult<NotificationDto>> GetNotification(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var notificationService = new NotificationService(_context, 
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationService>.Instance);
                
                var notification = await notificationService.GetNotificationByIdAsync(id, currentUserId);
                if (notification == null)
                {
                    return NotFound(new { error = "Notification not found" });
                }

                return Ok(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("notifications")]
        public async Task<ActionResult<NotificationDto>> CreateNotification([FromBody] CreateNotificationRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var notificationService = new NotificationService(_context, 
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationService>.Instance);
                var notification = await notificationService.CreateNotificationAsync(request);

                return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPut("notifications/{id}")]
        public async Task<ActionResult<NotificationDto>> UpdateNotification(int id, [FromBody] UpdateNotificationRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var notificationService = new NotificationService(_context, 
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationService>.Instance);
                
                var notification = await notificationService.UpdateNotificationAsync(id, request, currentUserId);
                if (notification == null)
                {
                    return NotFound(new { error = "Notification not found" });
                }

                return Ok(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpDelete("notifications/{id}")]
        public async Task<ActionResult> DeleteNotification(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var notificationService = new NotificationService(_context, 
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationService>.Instance);
                
                var success = await notificationService.DeleteNotificationAsync(id, currentUserId);
                if (!success)
                {
                    return NotFound(new { error = "Notification not found" });
                }

                return Ok(new { message = "Notification deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("notifications/{id}/mark-read")]
        public async Task<ActionResult> MarkNotificationAsRead(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var notificationService = new NotificationService(_context, 
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationService>.Instance);
                
                var success = await notificationService.MarkAsReadAsync(id, currentUserId);
                if (!success)
                {
                    return NotFound(new { error = "Notification not found or already read" });
                }

                return Ok(new { message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read {Id}", id);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("notifications/mark-all-read")]
        public async Task<ActionResult> MarkAllNotificationsAsRead()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var notificationService = new NotificationService(_context, 
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationService>.Instance);
                
                var success = await notificationService.MarkAllAsReadAsync(currentUserId);
                if (!success)
                {
                    return BadRequest(new { message = "No unread notifications found" });
                }

                return Ok(new { message = "All notifications marked as read" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("notifications/batch")]
        public async Task<ActionResult> BatchUpdateNotifications([FromBody] NotificationBatchRequest request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var notificationService = new NotificationService(_context, 
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationService>.Instance);
                
                var updatedCount = await notificationService.BatchUpdateNotificationsAsync(request, currentUserId);
                
                return Ok(new { 
                    message = $"Batch operation completed successfully",
                    updatedCount = updatedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing batch notification update");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpGet("notifications/stats")]
        public async Task<ActionResult<NotificationStatsDto>> GetNotificationStats()
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var notificationService = new NotificationService(_context, 
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationService>.Instance);
                
                var stats = await notificationService.GetNotificationStatsAsync(currentUserId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification stats");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("notifications/cleanup")]
        public async Task<ActionResult> CleanupExpiredNotifications()
        {
            try
            {
                var notificationService = new NotificationService(_context, 
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationService>.Instance);
                await notificationService.CleanupExpiredNotificationsAsync();

                return Ok(new { message = "Expired notifications cleaned up successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired notifications");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        #endregion

        #region Settings Management

        [HttpGet("settings")]
        public async Task<ActionResult<object>> GetAllSettings()
        {
            try
            {
                var settings = new
                {
                    general = await GetGeneralSettings(),
                    security = await GetSecuritySettings(),
                    backup = await GetBackupSettings(),
                    email = await GetEmailSettings(),
                    integrations = await GetIntegrationSettings(),
                    system = await GetSystemSettings()
                };

                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all settings");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("settings/{category}")]
        public async Task<ActionResult> SaveSettings(string category, [FromBody] object settingsData)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                switch (category.ToLower())
                {
                    case "general":
                        await SaveGeneralSettings(settingsData);
                        break;
                    case "security":
                        await SaveSecuritySettings(settingsData);
                        break;
                    case "backup":
                        await SaveBackupSettings(settingsData);
                        break;
                    case "email":
                        await SaveEmailSettings(settingsData);
                        break;
                    case "integrations":
                        await SaveIntegrationSettings(settingsData);
                        break;
                    default:
                        return BadRequest(new { error = "Invalid settings category" });
                }

                // Log the settings change
                await LogSettingsChange(currentUserId, category, settingsData);

                return Ok(new { message = $"{category} settings saved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving {category} settings", category);
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("settings/email/test")]
        public async Task<ActionResult> SendTestEmail()
        {
            try
            {
                var emailSettings = await GetEmailSettings();
                
                // Here you would implement actual email sending logic
                // For now, we'll simulate it
                await Task.Delay(1000); // Simulate email sending

                return Ok(new { message = "Test email sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email");
                return StatusCode(500, new { error = "Failed to send test email" });
            }
        }

        [HttpPost("backup/create")]
        public async Task<ActionResult<object>> CreateBackup()
        {
            try
            {
                var backupId = $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                
                // Here you would implement actual backup creation logic
                // For now, we'll simulate it
                await Task.Delay(2000); // Simulate backup creation

                return Ok(new { 
                    message = "Backup created successfully",
                    backupId = backupId,
                    createdAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating backup");
                return StatusCode(500, new { error = "Failed to create backup" });
            }
        }

        [HttpGet("backup/download/latest")]
        public async Task<IActionResult> DownloadLatestBackup()
        {
            try
            {
                // Here you would implement actual backup file retrieval
                // For now, we'll return a placeholder
                var backupContent = System.Text.Encoding.UTF8.GetBytes("Backup file content would be here");
                
                return File(backupContent, "application/zip", $"umihealth-backup-{DateTime.UtcNow:yyyyMMdd}.zip");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading backup");
                return StatusCode(500, new { error = "Failed to download backup" });
            }
        }

        [HttpGet("system/updates/check")]
        public async Task<ActionResult<object>> CheckForUpdates()
        {
            try
            {
                // Here you would implement actual update checking logic
                // For now, we'll simulate it
                await Task.Delay(1000);

                return Ok(new {
                    updateAvailable = false,
                    currentVersion = "2.1.0",
                    latestVersion = "2.1.0",
                    releaseNotes = "System is up to date"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for updates");
                return StatusCode(500, new { error = "Failed to check for updates" });
            }
        }

        [HttpGet("system/logs/download")]
        public async Task<IActionResult> DownloadSystemLogs()
        {
            try
            {
                // Here you would implement actual log file retrieval
                // For now, we'll return a placeholder
                var logContent = System.Text.Encoding.UTF8.GetBytes("System logs would be here");
                
                return File(logContent, "application/zip", $"umihealth-logs-{DateTime.UtcNow:yyyyMMdd}.zip");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading system logs");
                return StatusCode(500, new { error = "Failed to download system logs" });
            }
        }

        #endregion

        #region Settings Helper Methods

        private async Task<object> GetGeneralSettings()
        {
            var settings = await _context.SystemSettings
                .Where(s => s.Category == "General")
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            return new
            {
                systemName = GetSettingValue(settings, "systemName", _configuration["System:Name"] ?? "UmiHealth POS"),
                defaultCurrency = GetSettingValue(settings, "defaultCurrency", _configuration["System:DefaultCurrency"] ?? "Zambian Kwacha (K)"),
                timeZone = GetSettingValue(settings, "timeZone", _configuration["System:TimeZone"] ?? "CAT (UTC+2)"),
                companyName = GetSettingValue(settings, "companyName", _configuration["System:CompanyName"] ?? "UmiHealth Solutions"),
                supportEmail = GetSettingValue(settings, "supportEmail", _configuration["System:SupportEmail"] ?? "support@umihealth.com"),
                phoneNumber = GetSettingValue(settings, "phoneNumber", _configuration["System:PhoneNumber"] ?? "+260 123 456 789"),
                emailNotifications = bool.Parse(GetSettingValue(settings, "emailNotifications", _configuration["System:EmailNotifications"] ?? "true")),
                maintenanceMode = bool.Parse(GetSettingValue(settings, "maintenanceMode", _configuration["System:MaintenanceMode"] ?? "false")),
                debugMode = bool.Parse(GetSettingValue(settings, "debugMode", _configuration["System:DebugMode"] ?? "false"))
            };
        }

        private async Task<object> GetSecuritySettings()
        {
            var settings = await _context.SystemSettings
                .Where(s => s.Category == "Security")
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            return new
            {
                sessionTimeout = int.Parse(GetSettingValue(settings, "sessionTimeout", _configuration["Security:SessionTimeout"] ?? "30")),
                passwordPolicy = GetSettingValue(settings, "passwordPolicy", _configuration["Security:PasswordPolicy"] ?? "Strong (8+ chars, mixed case)"),
                maxLoginAttempts = int.Parse(GetSettingValue(settings, "maxLoginAttempts", _configuration["Security:MaxLoginAttempts"] ?? "5")),
                lockoutDuration = int.Parse(GetSettingValue(settings, "lockoutDuration", _configuration["Security:LockoutDuration"] ?? "15")),
                passwordExpiry = int.Parse(GetSettingValue(settings, "passwordExpiry", _configuration["Security:PasswordExpiry"] ?? "90")),
                apiRateLimit = int.Parse(GetSettingValue(settings, "apiRateLimit", _configuration["Security:ApiRateLimit"] ?? "1000")),
                twoFactorAuth = bool.Parse(GetSettingValue(settings, "twoFactorAuth", _configuration["Security:TwoFactorAuth"] ?? "true")),
                apiRateLimiting = bool.Parse(GetSettingValue(settings, "apiRateLimiting", _configuration["Security:ApiRateLimiting"] ?? "true")),
                ipWhitelisting = bool.Parse(GetSettingValue(settings, "ipWhitelisting", _configuration["Security:IpWhitelisting"] ?? "true")),
                forceHttps = bool.Parse(GetSettingValue(settings, "forceHttps", _configuration["Security:ForceHttps"] ?? "false"))
            };
        }

        private async Task<object> GetBackupSettings()
        {
            var settings = await _context.SystemSettings
                .Where(s => s.Category == "Backup")
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            return new
            {
                backupFrequency = GetSettingValue(settings, "backupFrequency", _configuration["Backup:Frequency"] ?? "Daily"),
                backupRetention = int.Parse(GetSettingValue(settings, "backupRetention", _configuration["Backup:Retention"] ?? "30")),
                backupLocation = GetSettingValue(settings, "backupLocation", _configuration["Backup:Location"] ?? "/backups"),
                compressionLevel = GetSettingValue(settings, "compressionLevel", _configuration["Backup:CompressionLevel"] ?? "High"),
                encryption = GetSettingValue(settings, "encryption", _configuration["Backup:Encryption"] ?? "AES-256"),
                cloudStorage = GetSettingValue(settings, "cloudStorage", _configuration["Backup:CloudStorage"] ?? "AWS S3"),
                autoBackup = bool.Parse(GetSettingValue(settings, "autoBackup", _configuration["Backup:AutoBackup"] ?? "true")),
                backupVerification = bool.Parse(GetSettingValue(settings, "backupVerification", _configuration["Backup:BackupVerification"] ?? "true")),
                emailBackupReports = bool.Parse(GetSettingValue(settings, "emailBackupReports", _configuration["Backup:EmailBackupReports"] ?? "false"))
            };
        }

        private async Task<object> GetEmailSettings()
        {
            var settings = await _context.SystemSettings
                .Where(s => s.Category == "Email")
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            return new
            {
                smtpServer = GetSettingValue(settings, "smtpServer", _configuration["Email:SmtpServer"] ?? "smtp.gmail.com"),
                port = int.Parse(GetSettingValue(settings, "port", _configuration["Email:Port"] ?? "587")),
                username = GetSettingValue(settings, "username", _configuration["Email:Username"] ?? "noreply@umihealth.com"),
                password = "•••••••••", // Never return actual password
                fromEmail = GetSettingValue(settings, "fromEmail", _configuration["Email:FromEmail"] ?? "noreply@umihealth.com"),
                fromName = GetSettingValue(settings, "fromName", _configuration["Email:FromName"] ?? "UmiHealth POS"),
                useSslTls = bool.Parse(GetSettingValue(settings, "useSslTls", _configuration["Email:UseSslTls"] ?? "true")),
                enableEmailLogging = bool.Parse(GetSettingValue(settings, "enableEmailLogging", _configuration["Email:EnableEmailLogging"] ?? "true"))
            };
        }

        private async Task<object> GetIntegrationSettings()
        {
            var settings = await _context.SystemSettings
                .Where(s => s.Category == "Integrations")
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            return new
            {
                paymentGateway = new
                {
                    provider = GetSettingValue(settings, "paymentProvider", _configuration["Integrations:PaymentGateway:Provider"] ?? "Stripe"),
                    apiKey = "•••••••••" // Never return actual API key
                },
                smsService = new
                {
                    provider = GetSettingValue(settings, "smsProvider", _configuration["Integrations:SmsService:Provider"] ?? "Twilio"),
                    apiKey = "•••••••••" // Never return actual API key
                },
                cloudStorage = new
                {
                    provider = GetSettingValue(settings, "storageProvider", _configuration["Integrations:CloudStorage:Provider"] ?? "AWS S3"),
                    accessKey = "•••••••••" // Never return actual access key
                }
            };
        }

        private async Task<object> GetSystemSettings()
        {
            return new
            {
                version = "2.1.0",
                lastUpdated = "Jan 10, 2024",
                database = "PostgreSQL 14.2",
                environment = "Production",
                apiVersion = "v1.0.0",
                serverUptime = "99.9%",
                activeUsers = await _context.Users.CountAsync(u => u.IsActive),
                storageUsed = "45.2 GB / 100 GB" // This would be calculated based on actual storage usage
            };
        }

        private async Task SaveGeneralSettings(object settingsData)
        {
            await SaveSettingsToDatabase("General", settingsData);
        }

        private async Task SaveSecuritySettings(object settingsData)
        {
            await SaveSettingsToDatabase("Security", settingsData);
        }

        private async Task SaveBackupSettings(object settingsData)
        {
            await SaveSettingsToDatabase("Backup", settingsData);
        }

        private async Task SaveEmailSettings(object settingsData)
        {
            await SaveSettingsToDatabase("Email", settingsData);
        }

        private async Task SaveIntegrationSettings(object settingsData)
        {
            await SaveSettingsToDatabase("Integrations", settingsData);
        }

        private async Task SaveSettingsToDatabase(string category, object settingsData)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var settingsDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(System.Text.Json.JsonSerializer.Serialize(settingsData));
            
            foreach (var setting in settingsDict)
            {
                var existingSetting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.Category == category && s.Key == setting.Key);

                if (existingSetting != null)
                {
                    var oldValue = existingSetting.Value;
                    existingSetting.Value = setting.Value?.ToString() ?? string.Empty;
                    existingSetting.UpdatedAt = DateTime.UtcNow;
                    existingSetting.UpdatedBy = currentUserId;

                    // Log the change
                    await LogSettingsChangeToDatabase(currentUserId, category, setting.Key, oldValue, setting.Value?.ToString() ?? string.Empty, "Updated");
                }
                else
                {
                    var newSetting = new SystemSetting
                    {
                        Category = category,
                        Key = setting.Key,
                        Value = setting.Value?.ToString() ?? string.Empty,
                        DataType = GetDataType(setting.Value),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = currentUserId
                    };

                    _context.SystemSettings.Add(newSetting);

                    // Log the creation
                    await LogSettingsChangeToDatabase(currentUserId, category, setting.Key, "", setting.Value?.ToString() ?? string.Empty, "Created");
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task LogSettingsChangeToDatabase(string userId, string category, string key, string oldValue, string newValue, string action)
        {
            var auditLog = new SettingsAuditLog
            {
                Category = category,
                UserId = userId,
                Action = action,
                OldValue = oldValue,
                NewValue = newValue,
                Description = $"Setting '{key}' {action.ToLower()}",
                Timestamp = DateTime.UtcNow,
                IpAddress = GetClientIpAddress(),
                UserAgent = Request.Headers["User-Agent"].ToString()
            };

            _context.SettingsAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        private string GetSettingValue(Dictionary<string, string> settings, string key, string defaultValue)
        {
            return settings.TryGetValue(key, out var value) ? value : defaultValue;
        }

        private string GetDataType(object value)
        {
            if (value is bool) return "Boolean";
            if (value is int || value is long || value is double || value is decimal) return "Number";
            if (value is string) return "String";
            return "JSON";
        }

        private async Task LogSettingsChange(string userId, string category, object settingsData)
        {
            var logEntry = new
            {
                UserId = userId,
                Category = category,
                SettingsData = settingsData,
                Timestamp = DateTime.UtcNow,
                Action = "SettingsUpdated"
            };

            _logger.LogInformation("Settings change logged: {@logEntry}", logEntry);
        }

        #endregion

        #endregion
    }
}
