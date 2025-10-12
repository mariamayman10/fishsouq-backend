namespace FishShop.API.Entities;

public class ProductSize
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string SizeName { get; set; }
    public decimal Price { get; set; }
    public Product Product { get; set; }
    public ICollection<OrderProduct> OrderProducts { get; set; }
}