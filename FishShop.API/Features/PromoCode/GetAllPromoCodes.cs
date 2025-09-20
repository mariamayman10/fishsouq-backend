using Carter;
using FishShop.API.Contracts;
using FishShop.API.Database;
using FishShop.API.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FishShop.API.Features;

public static class GetAllPromoCodes
{
    // Query
    public record Query : IRequest<Result<List<Contracts.GetPromoCode>>> { }

    // Handler
    internal sealed class Handler(AppDbContext dbContext, ILogger<GetAllPromoCodesEndpoint> logger) : IRequestHandler<Query, Result<List<Contracts.GetPromoCode>>>
    {
        public async Task<Result<List<Contracts.GetPromoCode>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var promoCodes = await dbContext.PromoCodes
                .Select(pc => new Contracts.GetPromoCode
                {
                    Id = pc.Id,
                    Code = pc.Code,
                    DiscountType = pc.DiscountType.ToString(),
                    DiscountValue = pc.DiscountValue,
                    TimesUsed = pc.TimesUsed,
                    IsActive = pc.IsActive,
                    CreatedAt = pc.CreatedAt,
                })
                .ToListAsync(cancellationToken);

            return Result.Success(promoCodes);
        }
    }
}

public class GetAllPromoCodesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/promo-code", async (ISender sender) =>
            {
                var result = await sender.Send(new GetAllPromoCodes.Query());
                return result.Resolve();
            })
            .RequireAuthorization(PolicyConstants.ManagerPolicy)
            .WithName("Get All Promo Codes");
    }
}