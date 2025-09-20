namespace FishShop.API.Entities;

public class OrderProduct
{
    public int OrderId { get; init; }
    public int ProductId { get; init; }

    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }

    public Order? Order { get; init; }
    public Product? Product { get; init; }
}