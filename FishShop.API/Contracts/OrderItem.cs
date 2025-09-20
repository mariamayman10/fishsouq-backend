namespace FishShop.API.Contracts;

public record OrderItem
{
    public int ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}