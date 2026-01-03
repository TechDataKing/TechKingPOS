using TechKingPOS.App.Models;

namespace TechKingPOS.App.Models
{
    public class Worker
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NationalId { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        // AUTH
        public string PasswordHash { get; set; }
        public int IsActive { get; set; }

        // 🔐 ROLE
        public UserRole Role { get; set; }
        public int MustChangePassword { get; set; }
        public int BranchId { get; set; }


    }
}
