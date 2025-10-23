using System.ComponentModel.DataAnnotations.Schema;

namespace FishShop.API.Contracts;

public class ProductDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public decimal Quantity { get; set; }
    public string? Description { get; set; }
    public string ImageUrl { get; set; }
    public decimal TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<ProductSizeDto>? Sizes { get; set; } = [];
}
