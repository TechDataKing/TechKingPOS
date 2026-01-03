using TechKingPOS.App.Services;

namespace TechKingPOS.App.Security
{
    public static class SessionContext
    {   public static int CurrentBranchId { get; set; }

        public static int? CurrentUserId =>
            SessionService.CurrentUser?.Id;

        public static string CurrentUserName =>
            SessionService.CurrentUser?.Name ?? "SYSTEM";

        public static bool IsLoggedIn =>
            SessionService.CurrentUser != null;
    }
}
