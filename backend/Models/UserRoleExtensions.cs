using System.ComponentModel.DataAnnotations;

namespace UmiHealthPOS.Models
{
    public static class UserRoleExtensions
    {
        public static string GetDisplayName(UserRoleEnum role)
        {
            return role switch
            {
                UserRoleEnum.SuperAdmin => "Super Admin",
                UserRoleEnum.TenantAdmin => "Tenant Admin",
                UserRoleEnum.Pharmacist => "Pharmacist",
                UserRoleEnum.Cashier => "Cashier",
                UserRoleEnum.Sales => "Sales Operations",
                _ => role.ToString()
            };
        }

        public static bool CanImpersonateRole(this UserRoleEnum adminRole, UserRoleEnum targetRole)
        {
            // Super Admin can impersonate anyone
            if (adminRole == UserRoleEnum.SuperAdmin)
                return true;

            // Tenant Admin can impersonate users within their tenant (except other Tenant Admins)
            if (adminRole == UserRoleEnum.TenantAdmin)
                return targetRole != UserRoleEnum.SuperAdmin && targetRole != UserRoleEnum.TenantAdmin;

            // Sales Operations can impersonate for demo purposes
            if (adminRole == UserRoleEnum.Sales)
                return targetRole == UserRoleEnum.Pharmacist || targetRole == UserRoleEnum.Cashier;

            // Other roles cannot impersonate
            return false;
        }
    }
}
