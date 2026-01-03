using TechKingPOS.App.Models;
using TechKingPOS.App.Security;


namespace TechKingPOS.App.Services
{
    public static class SessionService
    {
        public static Worker CurrentUser { get; private set; }

        public static bool IsLoggedIn => CurrentUser != null;

        public static void Login(Worker user)
        {       CurrentUser = user;    

                UserSession.UserId = user.Id;
                UserSession.UserName = user.Name;
                UserSession.Role = user.Role;

                SessionContext.CurrentBranchId = user.BranchId;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }
    }
}
