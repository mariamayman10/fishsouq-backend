namespace FishShop.API.Contracts;

public class Address
{
    public string Governorate { get; init; }
    public string Area { get; init; }
    public string Street { get; init; }
    public string BuildingNumber { get; init; }
    public string ApartmentNumber { get; init; }
    public string FloorNumber { get; init; }
}