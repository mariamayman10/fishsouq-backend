namespace FishShop.API.Contracts;

public record ProductDetails
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal Quantity { get; set; }
    public int CategoryId { get; set; }
    public List<ProductSizeDto>? Sizes { get; set; }
    public string ImageUrl { get; init; }
}