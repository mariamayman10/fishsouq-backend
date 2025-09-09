using FishShop.API.Entities.Enums;

namespace FishShop.API.Entities;

public class Order
{
    public int Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime CompletedAt { get; init; }
    public decimal TotalPrice { get; init; }
    public int DeliveryFees { get; set; }
    public decimal Discount { get; set; }
    public string PromoCode { get; set; }
    public DateTime DeliveryDate { get; set; }
    public OrderStatus Status { get; set; }
    public DeliveryType DeliveryType { get; init; }
    public string? UserId { get; init; }
    public string AddressId { get; init; }
    public User? User { get; init; }
    public string? PaymentInfo { get; set; }


    public ICollection<OrderProduct>? Products { get; init; }
}