using Carter;
using FishShop.API.Database;
using FishShop.API.Shared;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace FishShop.API.Features;

public static class UpdateProduct
{
    public record Command : IRequest<Result>
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal? Price { get; set; }
        public int? Quantity { get; set; }
        public string? Description { get; set; }
        public int? CategoryId { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(-1)
                .WithMessage("Invalid product ID");

            When(x => x.Name != null, () =>
            {
                RuleFor(x => x.Name)
                    .NotEmpty()
                    .WithMessage("Name cannot be empty when provided")
                    .MaximumLength(LengthConstants.ProductName)
                    .WithMessage($"Name cannot exceed {LengthConstants.ProductName} characters");
            });

            When(x => x.Price.HasValue, () =>
            {
                RuleFor(x => x.Price)
                    .GreaterThan(-1)
                    .WithMessage("Price must be greater than 0");
            });

            When(x => x.Quantity.HasValue, () =>
            {
                RuleFor(x => x.Quantity)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Quantity cannot be negative");
            });

            When(x => x.Description != null, () =>
            {
                RuleFor(x => x.Description)
                    .MaximumLength(LengthConstants.ProductDescription)
                    .WithMessage($"Description cannot exceed {LengthConstants.ProductDescription} characters");
            });

            When(x => x.CategoryId.HasValue, () =>
            {
                RuleFor(x => x.CategoryId)
                    .GreaterThan(-1)
                    .WithMessage("Invalid category ID");
            });
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<UpdateProductEndpoint> logger) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Attempting to update product {ProductId}", request.Id);


            var product = await dbContext.Products
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (product == null)
            {
                logger.LogWarning("Product {ProductId} not found or is deleted", request.Id);
                return Result.NotFound( "Product not found");
            }

            if (request.CategoryId.HasValue)
            {
                var categoryExists = await dbContext.Categories
                    .AnyAsync(x => x.Id == request.CategoryId && !x.IsDeleted, cancellationToken);

                if (!categoryExists)
                {
                    logger.LogWarning("Category {CategoryId} not found or is deleted", request.CategoryId);
                    return Result.NotFound( "Category not found");
                }
            }

            if (request.Name != null)
            {
                var nameExists = await dbContext.Products
                    .AnyAsync(x => x.Name == request.Name &&
                                   x.Id != request.Id &&
                                   !x.IsDeleted,
                        cancellationToken);

                if (nameExists)
                {
                    logger.LogWarning("Product name '{ProductName}' already exists", request.Name);
                    return Result.BadRequest("A product with this name already exists");
                }
            }

            // Track original values for logging
            var originalValues = new
            {
                product.Name,
                product.Price,
                product.Quantity,
                product.Description,
                product.CategoryId
            };

            // Update product
            product.Name = request.Name ?? product.Name;
            product.Price = request.Price ?? product.Price;
            product.Quantity = request.Quantity ?? product.Quantity;
            product.Description = request.Description ?? product.Description;
            product.CategoryId = request.CategoryId ?? product.CategoryId;

            await dbContext.SaveChangesAsync(cancellationToken);


            logger.LogInformation(
                "Successfully updated product {ProductId}. Changes: {@Changes}",
                request.Id,
                new
                {
                    Original = originalValues,
                    New = new
                    {
                        product.Name,
                        product.Price,
                        product.Quantity,
                        product.Description,
                        product.CategoryId
                    }
                });

            return Result.Success();
        }
    }
}

public class UpdateProductEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("api/products/{id}", async (
                int id,
                Contracts.UpdateProduct request,
                ISender sender,
                ILogger<UpdateProductEndpoint> logger) =>
            {
                var command = request.Adapt<UpdateProduct.Command>();
                command.Id = id;

                logger.LogInformation(
                    "Received request to update product {ProductId} with data: {@RequestData}",
                    id,
                    request);

                var result = await sender.Send(command);

                if (!result.IsSuccess)
                {
                    logger.LogWarning("Failed to update product {ProductId}: {Error}", id, result.Error);
                    return Results.BadRequest(result.Error);
                }

                return Results.NoContent();
            })
            .RequireAuthorization(PolicyConstants.ManagerOrAdminPolicy)
            .WithName("UpdateProduct")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Update a product",
                Description = "Updates an existing product's details. Requires admin privileges."
            });
    }
}