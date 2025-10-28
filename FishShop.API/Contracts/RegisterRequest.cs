using FishShop.API.Entities.Enums;

namespace FishShop.API.Contracts;

public record RegisterRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string PhoneNumber { get; set; }
    public required string Name { get; set; }
    public Gender? Gender { get; set; }
    public int? Age { get; set; }
    public string? Governorate { get; init; }
    public string? Area { get; init; }
}