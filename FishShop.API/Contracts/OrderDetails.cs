using FishShop.API.Entities.Enums;

namespace FishShop.API.Contracts;

public record OrderDetails
{
    public int Id { get; set; }
    public required string AddressId { get; set; }
    public string UserName { get; set; }
    public string UserId { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required OrderStatus Status { get; set; }
    public int DeliveryFees { get; set; }
    public decimal Discount { get; set; }
    public string PromoCode { get; set; }
    public decimal TotalAmount { get; set; }
    public required List<OrderItem> Items { get; set; }
    
    public required string PaymentInfo { get; set; }
    public required DateTime DeliveryDate { get; set; }
}