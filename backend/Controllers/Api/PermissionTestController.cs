using Microsoft.AspNetCore.Mvc;
using UmiHealthPOS.Attributes;
using UmiHealthPOS.Models;

namespace UmiHealthPOS.Controllers.Api
{
    /// <summary>
    /// Test controller to demonstrate permission system usage
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PermissionTestController : ControllerBase
    {
        /// <summary>
        /// Test endpoint requiring inventory view permission
        /// </summary>
        [RequirePermission(PermissionConstants.INVENTORY_VIEW)]
        [HttpGet("inventory")]
        public IActionResult TestInventoryPermission()
        {
            return Ok(new { message = "Access granted - You have inventory view permission" });
        }

        /// <summary>
        /// Test endpoint requiring pharmacist role
        /// </summary>
        [RequireRole("Pharmacist", "TenantAdmin")]
        [HttpGet("pharmacist-only")]
        public IActionResult TestPharmacistRole()
        {
            return Ok(new { message = "Access granted - You have pharmacist or tenant admin role" });
        }

        /// <summary>
        /// Test endpoint requiring both permission and role
        /// </summary>
        [RequirePermissionAndRole(
            new[] { PermissionConstants.PATIENT_CREATE },
            new[] { "Pharmacist", "TenantAdmin" })]
        [HttpPost("patients")]
        public IActionResult TestPatientCreation()
        {
            return Ok(new { message = "Access granted - You can create patients" });
        }

        /// <summary>
        /// Test endpoint accessible by all authenticated users
        /// </summary>
        [HttpGet("public")]
        public IActionResult TestPublicEndpoint()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            return Ok(new
            {
                message = "This endpoint is accessible to all authenticated users",
                userId = userId,
                role = userRole
            });
        }

        /// <summary>
        /// Test endpoint to check current user permissions
        /// </summary>
        [HttpGet("my-permissions")]
        public IActionResult TestMyPermissions()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            // Get permissions from claims (added by middleware)
            var permissions = User.FindAll("permission")?.Select(p => p.Value).ToList() ?? new List<string>();

            return Ok(new
            {
                userId = userId,
                role = userRole,
                permissions = permissions,
                permissionCount = permissions.Count
            });
        }
    }
}
