namespace FishShop.API.Contracts;

public record CreateOrderItem
{
    public int ProductSizeId { get; init; }
    public int Quantity { get; init; }
}