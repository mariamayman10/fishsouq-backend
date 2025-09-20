using Carter;
using FishShop.API.Contracts;
using FishShop.API.Database;
using FishShop.API.Shared;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FishShop.API.Features;

public static class GetPromoCode
{
    // Query
    public record Query : IRequest<Result<Contracts.GetPromoCode>>
    {
        public string Code { get; set; }
    }

    // Validator
    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Code)
                .NotEmpty();
        }
    }

    // Handler
    internal sealed class Handler(AppDbContext dbContext, ILogger<GetPromoCodeEndpoint> logger)
        : IRequestHandler<Query, Result<Contracts.GetPromoCode>>
    {
        public async Task<Result<Contracts.GetPromoCode>> Handle(Query request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Handling GetPromoCode request for code {PromoCode}", request.Code);

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogInformation("Invalid promo code request");
                return Result.BadRequest<Contracts.GetPromoCode>(validationResult.ToString());
            }

            var promoCode = await dbContext.PromoCodes
                .Where(pc => pc.Code == request.Code)
                .Select(pc => new Contracts.GetPromoCode
                {
                    Code = pc.Code,
                    DiscountType = pc.DiscountType.ToString(),
                    DiscountValue = pc.DiscountValue,
                    TimesUsed = pc.TimesUsed,
                    IsActive = pc.IsActive,
                    CreatedAt = pc.CreatedAt
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (promoCode is null)
            {
                logger.LogInformation("Promo code {PromoCode} not found", request.Code);
                return Result.NotFound<Contracts.GetPromoCode>("Promo code not found.");
            }

            logger.LogInformation("Promo code {PromoCode} retrieved", request.Code);

            return Result.Success(promoCode);
        }
    }
}

// Endpoint
public class GetPromoCodeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/promo-code/{code}", async (
                string code,
                ISender sender) =>
            {
                var query = new GetPromoCode.Query
                {
                    Code = code
                };
                var result = await sender.Send(query);
                return result.Resolve();
            })
            .WithName("Get Promo Code");
    }
}
