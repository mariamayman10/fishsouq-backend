using FishShop.API.Shared;

namespace FishShop.API.Entities;

public class Product : BaseEntity
{
    public int Id { get; init; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; init; }

    public ProductSales? ProductSales { get; init; }
    public IEnumerable<OrderProduct>? OrderProducts { get; init; }
}