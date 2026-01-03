namespace TechKingPOS.App.Models
{
    public class ItemImportResult
    {
        public string Name { get; set; }
        public string Alias { get; set; }
        public string MarkedPrice { get; set; }
        public string SellingPrice { get; set; }
        public string Quantity { get; set; }
        public string UnitType { get; set; }
         public string UnitValue { get; set; } 
        public bool IsValid { get; set; }
        public string Error { get; set; }
        public int BranchId { get; set; }

    }
}
