namespace FishShop.API.Contracts;

public class ProductFlatDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int Quantity { get; set; }
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public string? SizeName { get; set; }
    public decimal? Price { get; set; }
}