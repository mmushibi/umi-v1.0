using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UmiHealthPOS.Data;
using UmiHealthPOS.Models;
using UmiHealthPOS.Security;
using UmiHealthPOS.Services;

namespace UmiHealthPOS.Controllers
{
    /// <summary>
    /// Base controller with automatic row-level security and audit logging
    /// </summary>
    [Authorize]
    public abstract class BaseController : ControllerBase
    {
        protected readonly ApplicationDbContext _context;
        protected readonly ILogger _logger;
        protected readonly IRowLevelSecurityService _securityService;
        protected readonly IAuditService _auditService;

        protected BaseController(
            ApplicationDbContext context,
            ILogger logger,
            IRowLevelSecurityService securityService,
            IAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _securityService = securityService;
            _auditService = auditService;
        }

        /// <summary>
        /// Gets the current security context for the authenticated user
        /// </summary>
        protected async Task<SecurityContext> GetSecurityContextAsync()
        {
            var rowLevelContext = await _securityService.GetSecurityContextAsync(User);
            return new SecurityContext
            {
                UserId = rowLevelContext.UserId,
                TenantId = rowLevelContext.TenantId,
                BranchId = rowLevelContext.BranchId,
                Role = rowLevelContext.Role,
                UserName = User.Identity?.Name,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers["User-Agent"].ToString(),
                RequestTime = DateTime.UtcNow,
                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,
                IsImpersonated = rowLevelContext.IsImpersonating,
                ImpersonatedByUserId = rowLevelContext.ImpersonatedByUserId,
                Permissions = rowLevelContext.Permissions
            };
        }

        /// <summary>
        /// Applies automatic tenant filtering to queries
        /// </summary>
        protected async Task<IQueryable<T>> ApplyTenantFilterAsync<T>(IQueryable<T> query) where T : class
        {
            return await _securityService.ApplyTenantFilter<T>(query, User);
        }

        /// <summary>
        /// Applies automatic branch filtering to queries
        /// </summary>
        protected async Task<IQueryable<T>> ApplyBranchFilterAsync<T>(IQueryable<T> query) where T : class
        {
            return await _securityService.ApplyBranchFilter<T>(query, User);
        }

        /// <summary>
        /// Applies both tenant and branch filtering to queries
        /// </summary>
        protected async Task<IQueryable<T>> ApplySecurityFiltersAsync<T>(IQueryable<T> query) where T : class
        {
            query = await ApplyTenantFilterAsync<T>(query);
            query = await ApplyBranchFilterAsync<T>(query);
            return query;
        }

        /// <summary>
        /// Logs user access with automatic security context
        /// </summary>
        protected async Task LogAccessAsync(string action, string resource, string? resourceId = null, object? details = null)
        {
            try
            {
                var securityContext = await GetSecurityContextAsync();
                await _auditService.LogAccessAsync(new UmiHealthPOS.Models.AccessLog
                {
                    UserId = securityContext.UserId,
                    Action = action,
                    Controller = resource,
                    HttpMethod = "GET",
                    Status = "Success",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    Timestamp = DateTime.UtcNow,
                    MetadataJson = $"Accessed {resource} via {action}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log access for action {Action} on resource {Resource}", action, resource);
            }
        }

        /// <summary>
        /// Validates if user has specific permission
        /// </summary>
        protected async Task<bool> HasPermissionAsync(string permission)
        {
            return await _securityService.HasPermissionAsync(User, permission);
        }

        /// <summary>
        /// Validates if user can access specific tenant
        /// </summary>
        protected async Task<bool> CanAccessTenantAsync(string tenantId)
        {
            return await _securityService.CanAccessTenantAsync(User, tenantId);
        }

        /// <summary>
        /// Validates if user can access specific branch
        /// </summary>
        protected async Task<bool> CanAccessBranchAsync(int? branchId)
        {
            return await _securityService.CanAccessBranchAsync(User, branchId);
        }

        /// <summary>
        /// Gets filtered entity by ID with security checks
        /// </summary>
        protected async Task<ActionResult<T>> GetSecureEntityAsync<T>(int id, string resourceName) where T : class
        {
            try
            {
                var query = _context.Set<T>().AsQueryable();
                query = await ApplySecurityFiltersAsync<T>(query);

                var entity = await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
                
                if (entity == null)
                {
                    await LogAccessAsync("READ_FAILED", resourceName, id.ToString(), "Entity not found or access denied");
                    return NotFound();
                }

                await LogAccessAsync("READ_SUCCESS", resourceName, id.ToString());
                return Ok(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing {ResourceName} with ID {Id}", resourceName, id);
                await LogAccessAsync("READ_ERROR", resourceName, id.ToString(), ex.Message);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Securely creates an entity with audit logging
        /// </summary>
        protected async Task<ActionResult<T>> CreateSecureEntityAsync<T>(T entity, string resourceName) where T : class
        {
            try
            {
                // Set tenant and branch context if applicable
                var securityContext = await GetSecurityContextAsync();
                
                var tenantProperty = typeof(T).GetProperty("TenantId");
                if (tenantProperty != null && tenantProperty.PropertyType == typeof(string))
                {
                    tenantProperty.SetValue(entity, securityContext.TenantId);
                }

                var branchProperty = typeof(T).GetProperty("BranchId");
                if (branchProperty != null && branchProperty.PropertyType == typeof(int?))
                {
                    branchProperty.SetValue(entity, securityContext.BranchId);
                }

                _context.Set<T>().Add(entity);
                await _context.SaveChangesAsync();

                var entityIdValue = EF.Property<int>(entity, "Id");
                await LogAccessAsync("CREATE_SUCCESS", resourceName, entityIdValue.ToString(), entity);

                var entityIdProperty = typeof(T).GetProperty("Id");
                if (entityIdProperty != null)
                {
                    var entityId = entityIdProperty.GetValue(entity);
                    return CreatedAtAction("GetSecureEntityAsync", new { id = entityId }, entity);
                }
                return CreatedAtAction("GetSecureEntityAsync", new { id = 0 }, entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating {ResourceName}", resourceName);
                await LogAccessAsync("CREATE_ERROR", resourceName, null, ex.Message);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Securely updates an entity with audit logging
        /// </summary>
        protected async Task<ActionResult> UpdateSecureEntityAsync<T>(int id, T entity, string resourceName) where T : class
        {
            try
            {
                var query = _context.Set<T>().AsQueryable();
                query = await ApplySecurityFiltersAsync<T>(query);

                var existingEntity = await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
                if (existingEntity == null)
                {
                    await LogAccessAsync("UPDATE_FAILED", resourceName, id.ToString(), "Entity not found or access denied");
                    return NotFound();
                }

                // Update ID property
                var entityIdProperty = typeof(T).GetProperty("Id");
                if (entityIdProperty != null)
                {
                    var newId = entityIdProperty.GetValue(entity);
                    entityIdProperty.SetValue(existingEntity, newId);
                }

                // Update other properties
                var properties = typeof(T).GetProperties()
                    .Where(p => p.Name != "Id" && p.CanWrite)
                    .ToList();

                foreach (var property in properties)
                {
                    var newValue = property.GetValue(entity);
                    var oldValue = property.GetValue(existingEntity);
                    
                    if (!Equals(newValue, oldValue))
                    {
                        property.SetValue(existingEntity, newValue);
                    }
                }

                await _context.SaveChangesAsync();
                await LogAccessAsync("UPDATE_SUCCESS", resourceName, id.ToString(), entity);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating {ResourceName} with ID {Id}", resourceName, id);
                await LogAccessAsync("UPDATE_ERROR", resourceName, id.ToString(), ex.Message);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Securely deletes an entity with audit logging
        /// </summary>
        protected async Task<ActionResult> DeleteSecureEntityAsync<T>(int id, string resourceName) where T : class
        {
            try
            {
                var query = _context.Set<T>().AsQueryable();
                query = await ApplySecurityFiltersAsync<T>(query);

                var entity = await query.FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
                if (entity == null)
                {
                    await LogAccessAsync("DELETE_FAILED", resourceName, id.ToString(), "Entity not found or access denied");
                    return NotFound();
                }

                _context.Set<T>().Remove(entity);
                await _context.SaveChangesAsync();

                await LogAccessAsync("DELETE_SUCCESS", resourceName, id.ToString(), entity);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting {ResourceName} with ID {Id}", resourceName, id);
                await LogAccessAsync("DELETE_ERROR", resourceName, id.ToString(), ex.Message);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
