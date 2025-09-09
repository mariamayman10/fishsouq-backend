using Carter;
using FishShop.API.Database;
using FishShop.API.Shared;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace FishShop.API.Features;

public static class UpdateCategory
{
    public record Command : IRequest<Result>
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(-1)
                .WithMessage("Invalid category ID");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Category name is required")
                .MaximumLength(LengthConstants.CategoryName)
                .WithMessage($"Category name cannot exceed {LengthConstants.CategoryName} characters");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<UpdateCategoryEndpoint> logger) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Attempting to update category {CategoryId}", request.Id);

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
                logger.LogWarning("Category {CategoryId} not found or is deleted", request.Id);
                return Result.NotFound( "Category not found");
            }

            // Check for duplicate name
            var nameExists = await dbContext.Categories
                .AnyAsync(x => x.Name == request.Name
                               && x.Id != request.Id
                               && !x.IsDeleted,
                    cancellationToken);

            if (nameExists)
            {
                logger.LogWarning("Category name '{CategoryName}' already exists", request.Name);
                return Result.BadRequest("A category with this name already exists");
            }

            var originalName = category.Name;
            category.Name = request.Name;

            await dbContext.SaveChangesAsync(cancellationToken);


            logger.LogInformation(
                "Successfully updated category {CategoryId} name from '{OldName}' to '{NewName}'",
                request.Id,
                originalName,
                request.Name);

            return Result.Success();
        }
    }
}

public class UpdateCategoryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("api/categories/{id}", async (
                int id,
                Contracts.UpdateCategory request,
                ISender sender,
                ILogger<UpdateCategoryEndpoint> logger) =>
            {
                var command = request.Adapt<UpdateCategory.Command>();
                command.Id = id;

                logger.LogInformation(
                    "Received request to update category {CategoryId} with name '{Name}'",
                    id,
                    request.Name);

                var result = await sender.Send(command);

                return result.Resolve();
            })
            .RequireAuthorization(PolicyConstants.ManagerOrAdminPolicy)
            .WithName("UpdateCategory")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Update a category",
                Description = "Updates an existing category's name. Requires admin privileges."
            });
    }
}