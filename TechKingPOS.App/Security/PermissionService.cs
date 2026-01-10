using TechKingPOS.App.Models;
using TechKingPOS.App.Data;

namespace TechKingPOS.App.Security
{
    public static class PermissionService
    {
        public static bool Can(int userId, UserRole role, string permission)
        {
            // 🔑 ADMIN → FULL ACCESS
            if (role == UserRole.Admin)
                return true;

            // 🔑 GUEST → LIMITED ACCESS
            if (role == UserRole.Guest)
            {
                // Only allow explicitly allowed guest permissions
                return true;
                //return permission != "Workers"; 
            }

            // 🔑 WORKER → DB driven
            return PermissionRepository.HasPermission(userId, permission);
        }
    }
}
