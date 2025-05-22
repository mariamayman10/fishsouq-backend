using FishShop.API.Entities.Enums;

namespace FishShop.API.Contracts;

public record RegisterRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string PhoneNumber { get; set; }
    public required string Name { get; set; }
    public required Gender Gender { get; set; }
    public required int Age { get; set; }
}