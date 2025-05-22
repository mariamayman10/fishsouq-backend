using Carter;
using FishShop.API.Database;
using FishShop.API.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Security.Claims;

namespace FishShop.API.Features;

public static class GetUserAddresses
{
    public record Query : IRequest<Result<List<UserAddressResponse>>>
    {
        public string? UserId { get; init; }
    }

    public class UserAddressResponse
    {
        public int Id { get; init; }
        public string? Name { get; init; }
        public string? Street { get; init; }
        public string? Governorate { get; init; }
        public string? BuildingNumber { get; init; }
        public string? AptNumber { get; init; }
        public string? FloorNumber { get; init; }
        public bool IsDefault { get; init; }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<GetUserAddressesEndpoint> logger)
        : IRequestHandler<Query, Result<List<UserAddressResponse>>>
    {
        public async Task<Result<List<UserAddressResponse>>> Handle(Query request, CancellationToken cancellationToken)
        {
            using var scope = logger.BeginScope("UserId: {UserId}", request.UserId);
            logger.LogInformation("Retrieving user addresses");

            var addresses = await dbContext.UserAddresses
                .Where(x => x.UserId == request.UserId)
                .Select(x => new UserAddressResponse
                {
                    Id = x.Id,
                    Name = x.Name,
                    Street = x.Street,
                    Governorate = x.Governorate,
                    BuildingNumber = x.BuildingNumber,
                    AptNumber = x.AptNumber,
                    FloorNumber = x.FloorNumber,
                    IsDefault = x.IsDefault
                })
                .ToListAsync(cancellationToken);

            logger.LogInformation("Found {Count} addresses", addresses.Count);

            return Result.Success(addresses);
        }
    }
}

public class GetUserAddressesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/addresses", async (
                ISender sender,
                ILogger<GetUserAddressesEndpoint> logger,
                ClaimsPrincipal user) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var query = new GetUserAddresses.Query { UserId = userId };
                var result = await sender.Send(query);

                return result.Resolve();
            })
            .RequireAuthorization()
            .WithName("GetUserAddresses")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Get user addresses",
                Description = "Retrieves all addresses for the authenticated user"
            });
    }
}