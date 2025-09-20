namespace FishShop.API.Entities.Enums;

public enum OrderStatus
{
    Pending = 0,
    Confirmed,
    OutForDelivery,
    AwaitingCustomer,
    Delivered,
    Cancelled,
    Rejected
}