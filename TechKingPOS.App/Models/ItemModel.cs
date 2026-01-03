namespace TechKingPOS.App.Models
{
    public class ItemModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Alias { get; set; }

        public decimal MarkedPrice { get; set; }
        public decimal SellingPrice { get; set; }

        public decimal Quantity { get; set; }              // number of packages
        public string UnitType { get; set; }           // pieces, kg, g, mg, l, ml
        public decimal? UnitValue { get; set; }        // NULL for pieces
        public int BranchId { get; set; }

    }
}
