namespace FishShop.API.Contracts;

public record GetCategory
{
    public int Id { get; set; }
    public required string Name { get; set; }
}