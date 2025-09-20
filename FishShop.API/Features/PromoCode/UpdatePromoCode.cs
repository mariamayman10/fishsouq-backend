using Carter;
using FishShop.API.Database;
using FishShop.API.Shared;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
namespace FishShop.API.Features;

public class UpdatePromoCode
{
    public record Command : IRequest<Result>
    {
        public int Id { get; set; }
        public decimal? DiscountValue { get; set; }
        public bool? IsActive { get; set; }
    }
    internal sealed class Handler(AppDbContext dbContext, ILogger<UpdatePromoCodeEndpoint> logger) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Attempting to update promo code {PromoCodeId}", request.Id);


            var promoCode = await dbContext.PromoCodes
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (promoCode == null)
            {
                logger.LogWarning("Promo Code {PromoCodeId} not found or is deleted", request.Id);
                return Result.NotFound( "Promo Code not found");
            }
            
            var originalValues = new
            {
                promoCode.DiscountValue,
                promoCode.IsActive,
            };

            // Update product
            promoCode.DiscountValue = request.DiscountValue ?? promoCode.DiscountValue;
            promoCode.IsActive = request.IsActive ?? promoCode.IsActive;

            await dbContext.SaveChangesAsync(cancellationToken);


            logger.LogInformation(
                "Successfully updated promo code {PromoCodeId}. Changes: {@Changes}",
                request.Id,
                new
                {
                    Original = originalValues,
                    New = new
                    {
                        promoCode.Code,
                        promoCode.DiscountType,
                        promoCode.DiscountValue,
                        promoCode.IsActive
                    }
                });

            return Result.Success();
        }
    }
}

public class UpdatePromoCodeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("api/promo-code/{id}", async (
                int id,
                Contracts.UpdatePromoCode request,
                ISender sender,
                ILogger<UpdatePromoCodeEndpoint> logger) =>
            {
                var command = request.Adapt<UpdatePromoCode.Command>();
                command.Id = id;

                logger.LogInformation(
                    "Received request to update promo code {PromoCodeId} with data: {@RequestData}",
                    id,
                    request);

                var result = await sender.Send(command);

                if (!result.IsSuccess)
                {
                    logger.LogWarning("Failed to update promo code {PromoCodeId}: {Error}", id, result.Error);
                    return Results.BadRequest(result.Error);
                }

                return Results.NoContent();
            })
            .RequireAuthorization(PolicyConstants.ManagerPolicy)
            .WithName("UpdatePromoCode")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Update a promo code",
                Description = "Updates an existing promo code's details. Requires manager privileges."
            });
    }
}