namespace FishShop.API.Contracts;

public record CreateOrderItem
{
    public int ProductSizeId { get; init; }
    public decimal Quantity { get; init; }
}