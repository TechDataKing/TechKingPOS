using TechKingPOS.App.Security;

namespace TechKingPOS.App.Security
{
    public static class PermissionService
    {
        public static bool CanOpen(string key)
        {
            // Admin → everything allowed
            if (UserSession.IsAdmin)
                return true;

            // Worker → allowed windows ONLY
            return key == "Sales"
                || key == "Items"
                || key == "Stock"
                || key == "Credit"
                || key == "Reports"
                || key == "Workers"
                || key=="Settings";
        }
    }
}
