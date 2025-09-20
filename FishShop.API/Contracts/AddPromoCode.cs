
using FishShop.API.Entities;

namespace FishShop.API.Contracts;

public class AddPromoCode
{
    public required string Code { get; set; }
    public required DiscountType DiscountType { get; set; }
    public required decimal DiscountValue { get; set; }
}