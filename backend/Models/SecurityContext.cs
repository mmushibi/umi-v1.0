using System.ComponentModel.DataAnnotations;

namespace UmiHealthPOS.Models
{
    public class SecurityContext
    {
        public string? UserId { get; set; }
        public string? TenantId { get; set; }
        public int? BranchId { get; set; }
        public UserRoleEnum Role { get; set; }
        public string? UserName { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime? RequestTime { get; set; }
        public bool IsAuthenticated { get; set; } = false;
        public bool IsImpersonated { get; set; } = false;
        public string? ImpersonatedByUserId { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }
}
