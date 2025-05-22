namespace FishShop.API.Contracts;

public record AddCategory
{
    public required string Name { get; set; }
}