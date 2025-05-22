using System.Net;

namespace FishShop.API.Shared;

public record Error(HttpStatusCode Code, string Message)
{
    public static readonly Error None = new(HttpStatusCode.OK, string.Empty);
}