namespace FishShop.API.Contracts;

public class GetPromoCode
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty; // or enum if you expose it
    public decimal DiscountValue { get; set; }
    public int TimesUsed { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}