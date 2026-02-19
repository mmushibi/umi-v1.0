using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealthPOS.Data;
using UmiHealthPOS.DTOs;
using UmiHealthPOS.Models;
using UmiHealthPOS.Hubs;

namespace UmiHealthPOS.Services
{
    public interface INotificationService
    {
        Task<List<NotificationDto>> GetNotificationsAsync(NotificationFilterDto filter, string? userId = null);
        Task<NotificationDto?> GetNotificationByIdAsync(int id, string? userId = null);
        Task<NotificationDto> CreateNotificationAsync(CreateNotificationRequest request);
        Task<NotificationDto?> UpdateNotificationAsync(int id, UpdateNotificationRequest request, string? userId = null);
        Task<bool> DeleteNotificationAsync(int id, string? userId = null);
        Task<NotificationStatsDto> GetNotificationStatsAsync(string? userId = null);
        Task<bool> MarkAsReadAsync(int id, string? userId = null);
        Task<bool> MarkAllAsReadAsync(string? userId = null);
        Task<int> BatchUpdateNotificationsAsync(NotificationBatchRequest request, string? userId = null);
        Task CleanupExpiredNotificationsAsync();
        
        // Subscription notification methods
        Task SendSubscriptionExpiredNotification(Subscription subscription);
        Task SendGracePeriodNotification(Subscription subscription);
        Task SendExpirationWarning(Subscription subscription, int days);
        
        // Usage tracking notification methods
        Task SendLimitExceededNotification(string tenantId, string limitType, double currentUsage, double limit);
        Task SendLimitApproachingNotification(string tenantId, string limitType, double currentUsage, double limit);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task<List<NotificationDto>> GetNotificationsAsync(NotificationFilterDto filter, string? userId = null)
        {
            try
            {
                var query = _context.Notifications.AsQueryable();

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

                // Filter for user-specific notifications (super admin sees global + their own)
                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(n => n.IsGlobal || n.UserId == userId);
                }
                else
                {
                    // If no userId provided, only show global notifications
                    query = query.Where(n => n.IsGlobal);
                }

                // Exclude expired notifications
                query = query.Where(n => !n.ExpiresAt.HasValue || n.ExpiresAt.Value > DateTime.UtcNow);

                // Order by creation date (newest first) and apply pagination
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

                return notifications;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications");
                throw;
            }
        }

        public async Task<NotificationDto?> GetNotificationByIdAsync(int id, string? userId = null)
        {
            try
            {
                var notification = await _context.Notifications
                    .Where(n => n.Id == id)
                    .Where(n => n.IsGlobal || n.UserId == userId)
                    .Where(n => !n.ExpiresAt.HasValue || n.ExpiresAt.Value > DateTime.UtcNow)
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
                    .FirstOrDefaultAsync();

                return notification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification with ID {Id}", id);
                throw;
            }
        }

        public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationRequest request)
        {
            try
            {
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

                // Send real-time notification
                await SendRealTimeNotificationAsync(notificationDto);

                _logger.LogInformation("Created notification: {Title} for user {UserId}", notification.Title, notification.UserId);
                return notificationDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
                throw;
            }
        }

        public async Task<NotificationDto?> UpdateNotificationAsync(int id, UpdateNotificationRequest request, string? userId = null)
        {
            try
            {
                var notification = await _context.Notifications
                    .Where(n => n.Id == id)
                    .Where(n => n.IsGlobal || n.UserId == userId)
                    .FirstOrDefaultAsync();

                if (notification == null)
                {
                    return null;
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

                _logger.LogInformation("Updated notification: {Id}", notification.Id);
                return notificationDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification with ID {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteNotificationAsync(int id, string? userId = null)
        {
            try
            {
                var notification = await _context.Notifications
                    .Where(n => n.Id == id)
                    .Where(n => n.IsGlobal || n.UserId == userId)
                    .FirstOrDefaultAsync();

                if (notification == null)
                {
                    return false;
                }

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted notification: {Id}", notification.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification with ID {Id}", id);
                throw;
            }
        }

        public async Task<NotificationStatsDto> GetNotificationStatsAsync(string? userId = null)
        {
            try
            {
                var query = _context.Notifications.AsQueryable();

                // Filter for user-specific notifications
                if (!string.IsNullOrEmpty(userId))
                {
                    query = query.Where(n => n.IsGlobal || n.UserId == userId);
                }
                else
                {
                    query = query.Where(n => n.IsGlobal);
                }

                // Exclude expired notifications
                query = query.Where(n => !n.ExpiresAt.HasValue || n.ExpiresAt.Value > DateTime.UtcNow);

                var total = await query.CountAsync();
                var unread = await query.CountAsync(n => !n.IsRead);
                var read = await query.CountAsync(n => n.IsRead);
                var globalNotifications = await query.CountAsync(n => n.IsGlobal);
                var userNotifications = await query.CountAsync(n => !n.IsGlobal && n.UserId != null);
                var tenantNotifications = await query.CountAsync(n => n.TenantId != null);
                var expiredNotifications = await _context.Notifications
                    .CountAsync(n => n.ExpiresAt.HasValue && n.ExpiresAt.Value <= DateTime.UtcNow);

                return new NotificationStatsDto
                {
                    TotalNotifications = total,
                    UnreadNotifications = unread,
                    ReadNotifications = read,
                    GlobalNotifications = globalNotifications,
                    UserNotifications = userNotifications,
                    TenantNotifications = tenantNotifications,
                    ExpiredNotifications = expiredNotifications
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification stats");
                throw;
            }
        }

        public async Task<bool> MarkAsReadAsync(int id, string? userId = null)
        {
            try
            {
                var notification = await _context.Notifications
                    .Where(n => n.Id == id)
                    .Where(n => n.IsGlobal || n.UserId == userId)
                    .FirstOrDefaultAsync();

                if (notification == null || notification.IsRead)
                {
                    return false;
                }

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked notification as read: {Id}", notification.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read with ID {Id}", id);
                throw;
            }
        }

        public async Task<bool> MarkAllAsReadAsync(string? userId = null)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => !n.IsRead)
                    .Where(n => n.IsGlobal || n.UserId == userId)
                    .Where(n => !n.ExpiresAt.HasValue || n.ExpiresAt.Value > DateTime.UtcNow)
                    .ToListAsync();

                if (notifications.Count == 0)
                {
                    return false;
                }

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked {Count} notifications as read", notifications.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                throw;
            }
        }

        public async Task<int> BatchUpdateNotificationsAsync(NotificationBatchRequest request, string? userId = null)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => request.NotificationIds.Contains(n.Id))
                    .Where(n => n.IsGlobal || n.UserId == userId)
                    .ToListAsync();

                if (notifications.Count == 0)
                {
                    return 0;
                }

                switch (request.Action.ToLower())
                {
                    case "mark_read":
                        foreach (var notification in notifications)
                        {
                            notification.IsRead = true;
                            notification.ReadAt = DateTime.UtcNow;
                        }
                        break;
                    case "mark_unread":
                        foreach (var notification in notifications)
                        {
                            notification.IsRead = false;
                            notification.ReadAt = null;
                        }
                        break;
                    case "delete":
                        _context.Notifications.RemoveRange(notifications);
                        break;
                    default:
                        throw new ArgumentException($"Invalid action: {request.Action}");
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Batch {Action} {Count} notifications", request.Action, notifications.Count);
                return notifications.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch updating notifications");
                throw;
            }
        }

        public async Task CleanupExpiredNotificationsAsync()
        {
            try
            {
                var expiredNotifications = await _context.Notifications
                    .Where(n => n.ExpiresAt.HasValue && n.ExpiresAt.Value <= DateTime.UtcNow)
                    .ToListAsync();

                if (expiredNotifications.Count > 0)
                {
                    _context.Notifications.RemoveRange(expiredNotifications);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cleaned up {Count} expired notifications", expiredNotifications.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired notifications");
            }
        }

        // Subscription notification implementations
        private async Task<string?> GetTenantAdminUserIdAsync(string tenantId)
        {
            try
            {
                // Look for admin user for this tenant
                var adminUser = await _context.UserAccounts
                    .Where(u => u.TenantId == tenantId && 
                               (u.Role == "Admin" || u.Role == "SuperAdmin" || u.Email.Contains("admin")))
                    .FirstOrDefaultAsync();
                
                return adminUser?.Id.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding admin user for tenant {TenantId}", tenantId);
                return null;
            }
        }

        public async Task SendSubscriptionExpiredNotification(Subscription subscription)
        {
            try
            {
                var adminUserId = await GetTenantAdminUserIdAsync(subscription.TenantId);
                
                var notification = new CreateNotificationRequest
                {
                    Title = "Subscription Expired",
                    Message = $"Subscription for {subscription.Tenant?.Name} has expired. Please renew to continue service.",
                    Type = "subscription",
                    Category = "expiration",
                    IsGlobal = false,
                    UserId = adminUserId,
                    ExpiresAt = DateTime.UtcNow.AddDays(30)
                };

                await CreateNotificationAsync(notification);
                _logger.LogInformation("Sent subscription expired notification for tenant {TenantId}", subscription.TenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending subscription expired notification");
            }
        }

        public async Task SendGracePeriodNotification(Subscription subscription)
        {
            try
            {
                var adminUserId = await GetTenantAdminUserIdAsync(subscription.TenantId);
                
                var notification = new CreateNotificationRequest
                {
                    Title = "Grace Period Active",
                    Message = $"Subscription for {subscription.Tenant?.Name} is in grace period. You have limited time to renew.",
                    Type = "subscription",
                    Category = "grace_period",
                    IsGlobal = false,
                    UserId = adminUserId,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };

                await CreateNotificationAsync(notification);
                _logger.LogInformation("Sent grace period notification for tenant {TenantId}", subscription.TenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending grace period notification");
            }
        }

        public async Task SendExpirationWarning(Subscription subscription, int days)
        {
            try
            {
                var adminUserId = await GetTenantAdminUserIdAsync(subscription.TenantId);
                
                var notification = new CreateNotificationRequest
                {
                    Title = $"Subscription Expires in {days} Days",
                    Message = $"Subscription for {subscription.Tenant?.Name} will expire in {days} days. Please renew to avoid interruption.",
                    Type = "subscription",
                    Category = "warning",
                    IsGlobal = false,
                    UserId = adminUserId,
                    ExpiresAt = DateTime.UtcNow.AddDays(days + 1)
                };

                await CreateNotificationAsync(notification);
                _logger.LogInformation("Sent {Days}-day expiration warning for tenant {TenantId}", days, subscription.TenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending expiration warning");
            }
        }

        // Usage tracking notification implementations
        public async Task SendLimitExceededNotification(string tenantId, string limitType, double currentUsage, double limit)
        {
            try
            {
                var adminUserId = await GetTenantAdminUserIdAsync(tenantId);
                
                var notification = new CreateNotificationRequest
                {
                    Title = $"{limitType} Limit Exceeded",
                    Message = $"Your {limitType.ToLower()} usage ({currentUsage}) has exceeded the limit ({limit}). Please upgrade your plan.",
                    Type = "usage",
                    Category = "limit_exceeded",
                    IsGlobal = false,
                    UserId = adminUserId,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };

                await CreateNotificationAsync(notification);
                _logger.LogInformation("Sent limit exceeded notification for tenant {TenantId}, type {LimitType}", tenantId, limitType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending limit exceeded notification");
            }
        }

        public async Task SendLimitApproachingNotification(string tenantId, string limitType, double currentUsage, double limit)
        {
            try
            {
                var adminUserId = await GetTenantAdminUserIdAsync(tenantId);
                var percentageUsed = (currentUsage / limit) * 100;
                
                var notification = new CreateNotificationRequest
                {
                    Title = $"{limitType} Limit Warning",
                    Message = $"Your {limitType.ToLower()} usage is at {percentageUsed:F1}% ({currentUsage}/{limit}). Consider upgrading soon.",
                    Type = "usage",
                    Category = "limit_warning",
                    IsGlobal = false,
                    UserId = adminUserId,
                    ExpiresAt = DateTime.UtcNow.AddDays(3)
                };

                await CreateNotificationAsync(notification);
                _logger.LogInformation("Sent limit approaching notification for tenant {TenantId}, type {LimitType}", tenantId, limitType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending limit approaching notification");
            }
        }

        // Real-time notification methods
        private async Task SendRealTimeNotificationAsync(NotificationDto notification)
        {
            try
            {
                if (notification.IsGlobal)
                {
                    // Send to all connected users
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification);
                }
                else if (!string.IsNullOrEmpty(notification.UserId))
                {
                    // Send to specific user
                    await _hubContext.Clients.Group($"user_{notification.UserId}").SendAsync("ReceiveNotification", notification);
                }

                if (!string.IsNullOrEmpty(notification.TenantId))
                {
                    // Send to all users in the tenant
                    await _hubContext.Clients.Group($"tenant_{notification.TenantId}").SendAsync("ReceiveNotification", notification);
                }

                _logger.LogDebug("Sent real-time notification: {Title}", notification.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending real-time notification");
            }
        }

        // Notification template methods
        public async Task SendLowStockAlertAsync(string productId, string productName, int currentStock, int reorderLevel, string tenantId)
        {
            var notification = new CreateNotificationRequest
            {
                Title = "Low Stock Alert",
                Message = $"{productName} is running low on stock. Current: {currentStock}, Reorder at: {reorderLevel}",
                Type = "inventory",
                Category = "low_stock",
                TenantId = tenantId,
                IsGlobal = false,
                ActionUrl = $"/inventory/product/{productId}",
                ActionText = "View Product",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                Metadata = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["productId"] = productId,
                    ["productName"] = productName,
                    ["currentStock"] = currentStock,
                    ["reorderLevel"] = reorderLevel
                })
            };

            await CreateNotificationAsync(notification);
        }

        public async Task SendPrescriptionReadyNotificationAsync(string prescriptionId, string patientName, string userId)
        {
            var notification = new CreateNotificationRequest
            {
                Title = "Prescription Ready",
                Message = $"Prescription for {patientName} is ready for pickup",
                Type = "prescription",
                Category = "ready",
                UserId = userId,
                IsGlobal = false,
                ActionUrl = $"/prescriptions/{prescriptionId}",
                ActionText = "View Prescription",
                ExpiresAt = DateTime.UtcNow.AddDays(3),
                Metadata = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["prescriptionId"] = prescriptionId,
                    ["patientName"] = patientName
                })
            };

            await CreateNotificationAsync(notification);
        }

        public async Task SendSystemMaintenanceNotificationAsync(DateTime startTime, DateTime endTime, string description)
        {
            var notification = new CreateNotificationRequest
            {
                Title = "System Maintenance",
                Message = $"System will be under maintenance from {startTime:yyyy-MM-dd HH:mm} to {endTime:yyyy-MM-dd HH:mm}. {description}",
                Type = "system",
                Category = "maintenance",
                IsGlobal = true,
                ActionUrl = "/status",
                ActionText = "Check Status",
                ExpiresAt = endTime,
                Metadata = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["startTime"] = startTime,
                    ["endTime"] = endTime,
                    ["description"] = description
                })
            };

            await CreateNotificationAsync(notification);
        }

        public async Task SendWelcomeNotificationAsync(string userId, string userName, string tenantId)
        {
            var notification = new CreateNotificationRequest
            {
                Title = "Welcome to UMI Health POS!",
                Message = $"Welcome {userName}! Get started with our quick guide or explore the features.",
                Type = "system",
                Category = "welcome",
                UserId = userId,
                TenantId = tenantId,
                IsGlobal = false,
                ActionUrl = "/help/quick-start",
                ActionText = "Get Started",
                ExpiresAt = DateTime.UtcNow.AddDays(14),
                Metadata = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["userName"] = userName
                })
            };

            await CreateNotificationAsync(notification);
        }

        public async Task SendBackupCompletedNotificationAsync(string tenantId, bool success, string backupPath, long fileSize)
        {
            var notification = new CreateNotificationRequest
            {
                Title = success ? "Backup Completed Successfully" : "Backup Failed",
                Message = success 
                    ? $"System backup completed successfully. File size: {fileSize / 1024 / 1024:F1} MB"
                    : "System backup failed. Please check the system logs.",
                Type = "system",
                Category = "backup",
                TenantId = tenantId,
                IsGlobal = false,
                ActionUrl = "/settings/backup",
                ActionText = "View Backups",
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                Metadata = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["success"] = success,
                    ["backupPath"] = backupPath,
                    ["fileSize"] = fileSize
                })
            };

            await CreateNotificationAsync(notification);
        }
    }
}
