using Carter;
using FishShop.API.Database;
using FishShop.API.Entities.Enums;
using FishShop.API.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace FishShop.API.Features;

public static class DeleteProduct
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
                .WithMessage("Invalid product ID");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<DeleteProductEndpoint> logger) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Attempting to delete product {ProductId}", request.Id);

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogInformation("Invalid request, '{ProductId}'", request.Id);
                return Result.BadRequest(validationResult.ToString());
            }


            var product = await dbContext.Products
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (product == null)
            {
                logger.LogWarning("Product {ProductId} not found or is already deleted", request.Id);
                return Result.NotFound( "Product not found");
            }

            // Check if product is in any active orders
            var isInActiveOrders = await dbContext.OrderProducts
                .AnyAsync(x => x.ProductSize.ProductId == request.Id &&
                               x.Order!.Status != OrderStatus.Cancelled &&
                               x.Order.Status != OrderStatus.Delivered,
                    cancellationToken);

            if (isInActiveOrders)
            {
                logger.LogWarning("Cannot delete product {ProductId} as it is part of active orders", request.Id);
                return Result.BadRequest("Cannot delete product that is part of active orders");
            }

            // Soft delete
            product.IsDeleted = true;
            product.DeletedAt = DateTime.UtcNow;


            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Successfully deleted product {ProductId}", request.Id);

            return Result.Success();
        }
    }
}

public class DeleteProductEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/products/{id}", async (
                int id,
                ISender sender,
                ILogger<DeleteProductEndpoint> logger) =>
            {
                logger.LogInformation("Received request to delete product {ProductId}", id);

                var result = await sender.Send(new DeleteProduct.Command { Id = id });

                return result.Resolve();
            })
            .RequireAuthorization(PolicyConstants.ManagerOrAdminPolicy)
            .WithName("DeleteProduct")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Delete a product",
                Description = "Soft deletes a product if it's not part of any active orders. Requires admin privileges."
            });
    }
}