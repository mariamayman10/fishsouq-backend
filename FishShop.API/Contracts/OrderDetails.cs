using FishShop.API.Entities.Enums;

namespace FishShop.API.Contracts;

public record OrderDetails
{
    public int Id { get; set; }
    public required string AddressId { get; set; }
    public required string UserName { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public required List<OrderItem> Items { get; set; }
}