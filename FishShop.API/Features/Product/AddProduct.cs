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
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required int Quantity { get; set; }
        public required int CategoryId { get; set; }
        public required string ImageUrl { get; set; }

        // new field: dictionary of sizeName => price
        public required Dictionary<string, decimal> Sizes { get; set; }
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

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithMessage("Description is required")
                .MaximumLength(LengthConstants.ProductDescription)
                .WithMessage($"Description cannot exceed {LengthConstants.ProductDescription} characters");

            RuleFor(x => x.Quantity)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Quantity cannot be negative");

            RuleFor(x => x.CategoryId)
                .GreaterThan(0)
                .WithMessage("Invalid category ID");

            RuleFor(x => x.Sizes)
                .NotEmpty()
                .WithMessage("At least one size is required");

            RuleForEach(x => x.Sizes)
                .Must(s => s.Value > 0)
                .WithMessage("Each size must have a positive price");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<AddProductEndpoint> logger)
        : IRequestHandler<Command, Result<int>>
    {
        public async Task<Result<int>> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Attempting to create new product '{ProductName}'", request.Name);

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                return Result.BadRequest<int>(validationResult.ToString());

            var category = await dbContext.Categories
                .FirstOrDefaultAsync(x => x.Id == request.CategoryId && !x.IsDeleted, cancellationToken);

            if (category == null)
                return Result.NotFound<int>("Category not found");

            var nameExists = await dbContext.Products
                .AnyAsync(x => x.Name == request.Name && !x.IsDeleted, cancellationToken);

            if (nameExists)
                return Result.BadRequest<int>("A product with this name already exists");

            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Quantity = request.Quantity,
                CategoryId = request.CategoryId,
                ImageUrl = request.ImageUrl,
                Sizes = new List<ProductSize>()
            };

            // Map sizes
            foreach (var size in request.Sizes)
            {
                product.Sizes.Add(new ProductSize
                {
                    SizeName = size.Key,
                    Price = size.Value,
                    Product = product
                });
            }

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Product {ProductId} created with {SizeCount} sizes", product.Id, product.Sizes.Count);

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
                logger.LogInformation("Received request to create product '{ProductName}'", request.Name);

                var command = request.Adapt<AddProduct.Command>();
                var result = await sender.Send(command);

                return result.Resolve();
            })
            .RequireAuthorization(PolicyConstants.ManagerOrAdminPolicy)
            .WithName("AddProduct")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Create a new product with sizes",
                Description = "Creates a new product where each size can have its own price. Requires admin privileges."
            });
    }
}
