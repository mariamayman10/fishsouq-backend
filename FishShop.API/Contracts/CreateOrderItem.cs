namespace FishShop.API.Contracts;

public record CreateOrderItem
{
    public int ProductId { get; init; }
    public int Quantity { get; init; }
}