using TechKingPOS.App.Models;
using TechKingPOS.App.Security;

namespace TechKingPOS.App.Services
{
    public static class SessionService
    {
        public static Worker CurrentUser { get; private set; }

        public static bool IsLoggedIn => CurrentUser != null;

        public static void Login(Worker user)
        {
            CurrentUser = user;

            UserSession.UserId = user.Id;
            UserSession.UserName = user.Name;
            UserSession.Role = user.Role;

            SessionContext.CurrentBranchId = user.BranchId;
        }

        // 🔑 GUEST MODE
        public static void LoginAsGuest()
        {
            CurrentUser = new Worker
            {
                Id = 0,
                Name = "Guest",
                Role = UserRole.Guest,
                BranchId = 0
            };

            UserSession.UserId = 0;
            UserSession.UserName = "Guest";
            UserSession.Role = UserRole.Guest;

            SessionContext.CurrentBranchId = 0;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }
    }
}
