using Carter;
using FishShop.API.Database;
using FishShop.API.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore; 
using Microsoft.OpenApi.Models;
namespace FishShop.API.Features.PromoCode;

public class DeletePromoCode
{
    public record Command : IRequest<Result>
    {
        public int Id { get; init; }
    }
    
public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(-1)
                .WithMessage("Invalid promo code ID");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<DeletePromoCodeEndpoint> logger) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Attempting to delete promo code {id}", request.Id);

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogInformation("Invalid request, '{CategoryId}'", request.Id);
                return Result.BadRequest(validationResult.ToString());
            }


            var promoCode = await dbContext.PromoCodes
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (promoCode == null)
            {
                logger.LogWarning("Promo Code {id} not found or is already deleted", request.Id);
                return Result.NotFound("Promo code not found");
            }

           
            // Soft delete
            promoCode.IsDeleted = true;
            promoCode.DeletedAt = DateTime.UtcNow;


            await dbContext.SaveChangesAsync(cancellationToken);


            logger.LogInformation("Successfully deleted promo code {id}", request.Id);

            return Result.Success();
        }
    }
}

public class DeletePromoCodeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/promo-code/{id}", async (
                int id,
                ISender sender,
                ILogger<DeletePromoCodeEndpoint> logger) =>
            {
                logger.LogInformation("Received request to delete promo code {id}", id);

                var result = await sender.Send(new DeletePromoCode.Command { Id = id });

                return result.Resolve();
            })
            .RequireAuthorization(PolicyConstants.ManagerPolicy)
            .WithName("Delete Promo code")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Delete a promo code",
                Description = "Soft deletes a promo code. Requires admin privileges."
            });
    }
}