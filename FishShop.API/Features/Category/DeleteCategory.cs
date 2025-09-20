using Carter;
using FishShop.API.Database;
using FishShop.API.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore; 
using Microsoft.OpenApi.Models;

namespace FishShop.API.Features;

public static class DeleteCategory
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
                .WithMessage("Invalid category ID");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<DeleteCategoryEndpoint> logger) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Attempting to delete category {CategoryId}", request.Id);

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogInformation("Invalid request, '{CategoryId}'", request.Id);
                return Result.BadRequest(validationResult.ToString());
            }


            var category = await dbContext.Categories
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (category == null)
            {
                logger.LogWarning("Category {CategoryId} not found or is already deleted", request.Id);
                return Result.NotFound("Category not found");
            }

            // Check for active products in this category
            var hasActiveProducts = await dbContext.Products
                .AnyAsync(x => x.CategoryId == request.Id && !x.IsDeleted, cancellationToken);

            if (hasActiveProducts)
            {
                logger.LogWarning("Cannot delete category {CategoryId} as it contains active products", request.Id);
                return Result.BadRequest("Cannot delete category that contains active products");
            }

            // Soft delete
            category.IsDeleted = true;
            category.DeletedAt = DateTime.UtcNow;


            await dbContext.SaveChangesAsync(cancellationToken);


            logger.LogInformation("Successfully deleted category {CategoryId}", request.Id);

            return Result.Success();
        }
    }
}

public class DeleteCategoryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/categories/{id}", async (
                int id,
                ISender sender,
                ILogger<DeleteCategoryEndpoint> logger) =>
            {
                logger.LogInformation("Received request to delete category {CategoryId}", id);

                var result = await sender.Send(new DeleteCategory.Command { Id = id });

                return result.Resolve();
            })
            .RequireAuthorization(PolicyConstants.ManagerOrAdminPolicy)
            .WithName("DeleteCategory")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Delete a category",
                Description = "Soft deletes a category if it has no active products. Requires admin privileges."
            });
    }
}