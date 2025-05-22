namespace FishShop.API.Contracts;

public record UpdateUserAddress
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Street { get; set; }
    public string? Governorate { get; set; }
    public string? BuildingNumber { get; set; }
    public string? AptNumber { get; set; }
    public string? FloorNumber { get; set; }
    public bool IsDefault { get; set; }
    public string UserId { get; set; }
}