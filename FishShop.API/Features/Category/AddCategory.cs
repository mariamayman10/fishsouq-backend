using Carter;
using FishShop.API.Database;
using FishShop.API.Entities;
using FishShop.API.Shared;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace FishShop.API.Features;

public static class AddCategory
{
    public record Command : IRequest<Result<string>>
    {
        public string? Name { get; set; }
    }

    private class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Category name is required")
                .MaximumLength(LengthConstants.CategoryName)
                .WithMessage($"Category name cannot exceed {LengthConstants.CategoryName} characters");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<AddCategoryEndpoint> logger) : IRequestHandler<Command, Result<string>>
    {
        public async Task<Result<string>> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Attempting to create new category with name '{CategoryName}'", request.Name);

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogInformation("Invalid request, '{CategoryName}'", request.Name);
                return Result.BadRequest<string>(validationResult.ToString());
            }
            
            // Check for existing category with same name (including soft-deleted ones)
            var nameExists = await dbContext.Categories
                .AnyAsync(x => x.Name == request.Name && !x.IsDeleted, cancellationToken);

            if (nameExists)
            {
                logger.LogWarning("Category name '{CategoryName}' already exists", request.Name);
                return Result.BadRequest<string>("A category with this name already exists");
            }

            var category = new Category
            {
                Name = request.Name
            };


            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync(cancellationToken);


            logger.LogInformation(
                "Successfully created category {CategoryId} with name '{CategoryName}'",
                category.Id,
                category.Name);

            return Result.Created(string.Empty);
        }
    }
}

public class AddCategoryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/categories", async (
                Contracts.AddCategory request,
                ISender sender,
                ILogger<AddCategoryEndpoint> logger) =>
            {
                logger.LogInformation(
                    "Received request to create category with name '{CategoryName}'",
                    request.Name);

                var command = request.Adapt<AddCategory.Command>();
                var result = await sender.Send(command);

                return result.Resolve();
            })
            .RequireAuthorization(PolicyConstants.ManagerOrAdminPolicy)
            .WithName("AddCategory")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Create a new category",
                Description = "Creates a new product category. Requires admin privileges."
            });
    }
}