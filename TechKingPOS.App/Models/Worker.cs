namespace TechKingPOS.App.Models
{
    public class Worker
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NationalId { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        // AUTH FIELDS
        public string PasswordHash { get; set; }
        public int IsActive { get; set; }
    }
}
