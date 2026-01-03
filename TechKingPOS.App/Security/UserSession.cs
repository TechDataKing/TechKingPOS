using TechKingPOS.App.Models;

namespace TechKingPOS.App.Security
{
    public static class UserSession
    {
        public static int UserId { get; set; }
        public static string UserName { get; set; }
        public static UserRole Role { get; set; }

        public static bool IsAdmin => Role == UserRole.Admin;
        public static bool IsWorker => Role == UserRole.Worker;
    }
}
