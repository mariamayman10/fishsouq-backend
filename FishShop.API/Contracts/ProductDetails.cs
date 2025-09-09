namespace FishShop.API.Contracts;

public record ProductDetails
{
    public int Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal Quantity { get; set; }
    public int CategoryId { get; set; }
    public decimal Price { get; init; }
    public string ImageUrl { get; init; }
}