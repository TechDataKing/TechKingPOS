namespace TechKingPOS.App.Models
{
    public class BranchView
    {
        public int Id { get; set; }
        public string Display { get; set; }  // "Main Branch (MAIN-001)"
        public bool IsActive { get; set; }
    }
}
