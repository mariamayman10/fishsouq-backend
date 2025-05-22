namespace FishShop.API.Shared;

public abstract class BaseEntity : ISoftDelete
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}