namespace FishShop.API.Shared;

public record PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public int TotalItems { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}