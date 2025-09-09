using System.Collections;
using FishShop.API.Entities;
using FishShop.API.Entities.Enums;

namespace FishShop.API.Contracts;

public record OrderOverview
{
    public int Id { get; init; }
    public string UserId { get; init; }
    public string UserName { get; init; }
    public decimal TotalPrice { get; init; }
    public DateTime CreatedAt { get; init; }
    public OrderStatus Status { get; init; }
    public DateTime DeliveryDate { get; set; }
    public DeliveryType DeliveryType { get; set; }
    public string DeliveryAddress { get; init; }
    public IEnumerable? Products { get; set; }
}