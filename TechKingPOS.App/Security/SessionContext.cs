using TechKingPOS.App.Services;
using TechKingPOS.App.Models;

namespace TechKingPOS.App.Security

{
    public static class SessionContext
    {
        public static int CurrentBranchId { get; set; } = 1; // Continue to track the current branch
        public static bool IsReportsWindowActive { get; set; } = false;
        public static int? CurrentUserId =>
            SessionService.CurrentUser?.Id;

        public static string CurrentUserName =>
            SessionService.CurrentUser?.Name ?? "SYSTEM";

        public static bool IsLoggedIn =>
            SessionService.CurrentUser != null;

        // 🔑 ROLE HELPERS
        public static bool IsAdmin =>
            SessionService.CurrentUser?.Role == UserRole.Admin;

        // 🔑 BRANCH RULES
        public static int EffectiveBranchId
        {
            get
            {
                // Admin can change branch dynamically
                if (IsAdmin)
                    return CurrentBranchId;  // Admins use the selected branch in the UI

                // Workers are locked to login branch (branch they logged into)
                return SessionService.CurrentUser?.BranchId ?? CurrentBranchId;
            }
        }
    }
}
