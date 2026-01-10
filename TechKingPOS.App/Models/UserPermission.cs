namespace TechKingPOS.App.Models
{
    public class UserPermission
    {
        public int UserId { get; set; }
        public string PermissionKey { get; set; }
        public bool Granted { get; set; }
    }
}
