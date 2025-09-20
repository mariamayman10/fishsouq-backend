using FishShop.API.Entities.Enums;

namespace FishShop.API.Contracts;

public record CreateOrder
{
    public int Id { get; init; }
    public decimal TotalPrice { get; init; }
    public DateTime CreatedAt { get; init; }
    public OrderStatus Status { get; init; }
    public DeliveryType DeliveryType { get; set; }
    public int DeliveryFees { get; init; }
    public decimal Discount { get; init; }
    public string PromoCode { get; init; }
    public List<CreateOrderItem>? Items { get; set; }
    public string? AddressId { get; init; }
    public string? PaymentInfo {get; init; }
}