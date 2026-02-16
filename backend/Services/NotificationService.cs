using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UmiHealthPOS.Data;
using UmiHealthPOS.DTOs;
using UmiHealthPOS.Models;

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
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
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

                if (!notifications.Any())
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

                if (!notifications.Any())
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

                if (expiredNotifications.Any())
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
    }
}
