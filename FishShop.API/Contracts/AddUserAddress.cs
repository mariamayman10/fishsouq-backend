namespace FishShop.API.Contracts;

public record AddUserAddress
{
    public string Name { get; init; }
    public string Street { get; set; }
    public string Governorate { get; set; }
    public string BuildingNumber { get; set; }
    public string AptNumber { get; set; }
    public string FloorNumber { get; set; }
    public bool IsDefault { get; set; }
    public string UserId { get; set; }
}