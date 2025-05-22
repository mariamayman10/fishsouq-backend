using FishShop.API.Entities.Enums;

namespace FishShop.API.Entities;

public class Order
{
    public int Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime CompletedAt { get; init; }
    public decimal TotalPrice { get; init; }
    public DateTime DeliveryDate { get; init; }
    public OrderStatus Status { get; set; }
    public DeliveryType DeliveryType { get; init; }
    public string? UserId { get; init; }
    public string AddressId { get; init; }
    public User? User { get; init; }

    public ICollection<OrderProduct>? Products { get; init; }
}