using FishShop.API.Entities.Enums;

namespace FishShop.API.Contracts;

public record CreateOrder
{
    public int Id { get; init; }
    public decimal TotalPrice { get; init; }
    public DateTime CreatedAt { get; init; }
    public OrderStatus Status { get; init; }
    public DateTime DeliveryDate { get; set; }
    public DeliveryType DeliveryType { get; set; }
    public List<CreateOrderItem>? Items { get; set; }
    public string? AddressId { get; init; }
}