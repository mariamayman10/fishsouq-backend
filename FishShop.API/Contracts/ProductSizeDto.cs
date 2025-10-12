namespace FishShop.API.Contracts;

public class ProductSizeDto
{
    public int Id { get; set; }
    public string SizeName { get; set; } = "Regular";
    public decimal Price { get; set; }
}