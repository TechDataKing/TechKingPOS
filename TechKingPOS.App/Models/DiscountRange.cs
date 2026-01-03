public class DiscountRange
{   public int Id { get; set; }
    public decimal From { get; set; }
    public decimal To { get; set; }

    public decimal? DiscountPercent { get; set; }
    public decimal? DiscountAmount { get; set; }
    public int BranchId { get; set; }

}
