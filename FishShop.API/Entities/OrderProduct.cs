namespace FishShop.API.Entities;

public class OrderProduct
{
    public int OrderId { get; set; }
    public int ProductSizeId { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public Order Order { get; set; } = null!;
    public ProductSize ProductSize { get; set; } = null!;
}