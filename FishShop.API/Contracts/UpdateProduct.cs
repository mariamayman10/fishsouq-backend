namespace FishShop.API.Contracts;

public record UpdateProduct
{
    public string? Name { get; set; }
    public decimal? Price { get; set; }
    public int? Quantity { get; set; }
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
}