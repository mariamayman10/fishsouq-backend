using Microsoft.AspNetCore.Identity;

namespace FishShop.API.Entities;

public class User : IdentityUser
{
    public ICollection<RefreshToken> RefreshTokens { get; init; } = new List<RefreshToken>();

    public ICollection<Order> Orders { get; init; } = new List<Order>();
}