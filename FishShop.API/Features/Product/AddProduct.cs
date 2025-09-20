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

public static class AddProduct
{
    public record Command : IRequest<Result<int>>
    {
        public required string? Name { get; set; }
        public required decimal Price { get; set; }
        public required int Quantity { get; set; }
        public required string? Description { get; set; }
        public required int CategoryId { get; set; }
        public required string ImageUrl { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Product name is required")
                .MaximumLength(LengthConstants.ProductName)
                .WithMessage($"Product name cannot exceed {LengthConstants.ProductName} characters");

            RuleFor(x => x.Price)
                .GreaterThan(-1)
                .WithMessage("Price must be greater than 0");

            RuleFor(x => x.Quantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Quantity cannot be negative");

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required")
                .MaximumLength(LengthConstants.ProductDescription)
                .WithMessage($"Description cannot exceed {LengthConstants.ProductDescription} characters");

            RuleFor(x => x.CategoryId)
                .GreaterThan(-1)
                .WithMessage("Invalid category ID");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<AddProductEndpoint> logger) : IRequestHandler<Command, Result<int>>
    {
        public async Task<Result<int>> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation(
                "Attempting to create new product '{ProductName}' in category {CategoryId}",
                request.Name,
                request.CategoryId);

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogInformation("Invalid request");
                return Result.BadRequest<int>( validationResult.ToString());
            }


            // Verify category exists and is not deleted
            var category = await dbContext.Categories
                .FirstOrDefaultAsync(x => x.Id == request.CategoryId && !x.IsDeleted, cancellationToken);

            if (category == null)
            {
                logger.LogWarning("Category {CategoryId} not found or is deleted", request.CategoryId);
                return Result.NotFound<int>( "Category not found");
            }

            // Check for existing product with same name
            var nameExists = await dbContext.Products
                .AnyAsync(x => x.Name == request.Name && !x.IsDeleted, cancellationToken);

            if (nameExists)
            {
                logger.LogWarning("Product name '{ProductName}' already exists", request.Name);
                return Result.BadRequest<int>("A product with this name already exists");
            }

            var product = new Product
            {
                Name = request.Name,
                Price = request.Price,
                Quantity = request.Quantity,
                Description = request.Description,
                CategoryId = request.CategoryId,
                ImageUrl = request.ImageUrl
            };

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Successfully created product {ProductId} '{ProductName}' in category {CategoryId}",
                product.Id,
                product.Name,
                product.CategoryId);

            return Result.Success(product.Id);
        }
    }
}

public class AddProductEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/products", async (
                Contracts.AddProduct request,
                ISender sender,
                ILogger<AddProductEndpoint> logger) =>
            {
                logger.LogInformation(
                    "Received request to create product '{ProductName}' in category {CategoryId}",
                    request.Name,
                    request.CategoryId);

                var command = request.Adapt<AddProduct.Command>();
                var result = await sender.Send(command);

                return result.Resolve();
            })
            .RequireAuthorization(PolicyConstants.ManagerOrAdminPolicy)            
            .WithName("AddProduct")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Create a new product",
                Description = "Creates a new product in the specified category. Requires admin privileges."
            });
    }
}