public class DamagedItemVM
{   public int Id { get; set;}
    public int ItemId { get; set; }
    public string Name { get; set; } = "";
    public string Alias { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal SellingPrice { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int BranchId { get; set; }


    public string Display =>
        $"{Name} | Qty: {Quantity} | {Reason} | {CreatedAt:g}";
}
