using TechKingPOS.App.Models;

namespace TechKingPOS.App.Services
{
    public static class SessionService
    {
        public static Worker CurrentUser { get; private set; }

        public static bool IsLoggedIn => CurrentUser != null;

        public static void Login(Worker user)
        {
            CurrentUser = user;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }
    }
}
