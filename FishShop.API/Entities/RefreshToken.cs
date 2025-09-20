namespace FishShop.API.Entities;

public class RefreshToken
{
    public Guid Id { get; init; }
    public string? Token { get; init; } = null!;
    public DateTime ExpiryDate { get; init; }
    public bool IsRevoked { get; init; }
    public string? UserId { get; init; } = null!;
    public User User { get; init; } = null!;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}