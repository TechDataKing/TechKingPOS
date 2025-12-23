public class CartItemModel
{
    public string Name { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; }
    public decimal Price { get; set; }
    public decimal Total => Quantity * Price;
}
