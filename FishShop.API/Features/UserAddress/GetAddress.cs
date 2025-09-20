using Carter;
using FishShop.API.Database;
using FishShop.API.Entities;
using FishShop.API.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace FishShop.API.Features;

public static class GetAddress
{
    public record Query : IRequest<Result<UserAddress>>
    {
        public int Id { get; set; }
    }
    
    internal sealed class Handler(AppDbContext dbContext, ILogger<GetAddressEndpoint> logger) : IRequestHandler<Query, Result<UserAddress>>
    {
        public async Task<Result<UserAddress>> Handle(Query request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Handling GetAddress request for address {AddressId}",
                request.Id);
            var address = await dbContext.UserAddresses
                .Where(x => x.Id == request.Id)
                .Select(x => new UserAddress
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
                .FirstOrDefaultAsync(cancellationToken);

            return address is null
                ? Result.NotFound<UserAddress>("Address not found.")
                : Result.Success(address);
        }
    }
}
public class GetAddressEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/addresses/{id}", async (
                int id,
                ISender sender) =>
            {
                var query = new GetAddress.Query
                {
                    Id = id
                };
                var result = await sender.Send(query);
                return result.Resolve();
            })
            .RequireAuthorization(PolicyConstants.ManagerOrAdminPolicy)
            .WithName("GetAddressById")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Get Address By Id",
                Description = "Get Address By Id. Requires admin privileges."
            });
    }
}