using Carter;
using FishShop.API.Database;
using FishShop.API.Entities;
using FishShop.API.Shared;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
namespace FishShop.API.Features.PromoCode;

public class AddPromoCode
{
 public record Command : IRequest<Result<string>>
    {
        public required string Code { get; set; }
        public required DiscountType DiscountType { get; set; }
        public required decimal DiscountValue { get; set; }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<AddPromoCodeEndpoint> logger) : IRequestHandler<Command, Result<string>>
    {
        public async Task<Result<string>> Handle(Command request, CancellationToken cancellationToken)
        {
            
            // Check for existing category with same name (including soft-deleted ones)
            var codeExists = await dbContext.PromoCodes
                .AnyAsync(x => x.Code == request.Code && !x.IsDeleted, cancellationToken);
            
            if (codeExists)
            {
                logger.LogWarning("Promo Code '{PromoCode}' already exists", request.Code);
                return Result.BadRequest<string>("A category with this name already exists");
            }
            
            var promoCode = new Entities.PromoCode
            {
                Code = request.Code,
                DiscountType = request.DiscountType,
                DiscountValue = request.DiscountValue
            };


            dbContext.PromoCodes.Add(promoCode);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            
            logger.LogInformation(
                "Successfully created promo code {code id} with code '{code}'",
                promoCode.Id,
                promoCode.Code);

            return Result.Created(string.Empty);
        }
    }
}

public class AddPromoCodeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/promo-code", async (
                Contracts.AddPromoCode request,
                ISender sender,
                ILogger<AddPromoCodeEndpoint> logger) =>
            {
                logger.LogInformation(
                    "Received request to create promocode with code '{PromoCode}' with type {CodeType} discount value '{DiscountValue}'",
                    request.Code, request.DiscountType, request.DiscountValue);

                var command = request.Adapt<AddPromoCode.Command>();
                var result = await sender.Send(command);

                return result.Resolve();
            })
            .RequireAuthorization(PolicyConstants.ManagerPolicy)
            .WithName("AddPromoCode")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Create a new promo code",
                Description = "Creates a new promo code. Requires admin privileges."
            });
    }
}