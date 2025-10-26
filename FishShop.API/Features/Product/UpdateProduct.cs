using Carter;
using FishShop.API.Contracts;
using FishShop.API.Database;
using FishShop.API.Entities;
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
        public int? Quantity { get; set; }
        public string? Description { get; set; }
        public int? CategoryId { get; set; }
        public List<ProductSizeDto>? Sizes { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Invalid product ID");

            When(x => x.Name != null, () =>
            {
                RuleFor(x => x.Name)
                    .NotEmpty()
                    .MaximumLength(LengthConstants.ProductName)
                    .WithMessage($"Name cannot exceed {LengthConstants.ProductName} characters");
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
                    .GreaterThan(0)
                    .WithMessage("Invalid category ID");
            });

            When(x => x.Sizes != null, () =>
            {
                RuleForEach(x => x.Sizes!)
                    .Must(s => s.Price > 0)
                    .WithMessage("Each size must have a positive price");
            });
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<UpdateProductEndpoint> logger)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Attempting to update product {ProductId}", request.Id);

            var product = await dbContext.Products
                .Include(p => p.Sizes)
                .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

            if (product == null)
                return Result.NotFound("Product not found");

            if (request.CategoryId.HasValue)
            {
                var categoryExists = await dbContext.Categories
                    .AnyAsync(x => x.Id == request.CategoryId && !x.IsDeleted, cancellationToken);

                if (!categoryExists)
                    return Result.NotFound("Category not found");
            }

            if (request.Name != null)
            {
                var nameExists = await dbContext.Products
                    .AnyAsync(x => x.Name == request.Name && x.Id != request.Id && !x.IsDeleted, cancellationToken);

                if (nameExists)
                    return Result.BadRequest("A product with this name already exists");
            }

            // Update basic fields
            product.Name = request.Name ?? product.Name;
            product.Quantity = request.Quantity ?? product.Quantity;
            product.Description = request.Description ?? product.Description;
            product.CategoryId = request.CategoryId ?? product.CategoryId;

            // Update product sizes if provided
            if (request.Sizes != null)
            {
                foreach (var sizeDto in request.Sizes)
                {
                    // Case 1: Existing size -> update it
                    if (sizeDto.Id != 0)
                    {
                        var existing = product.Sizes.FirstOrDefault(s => s.Id == sizeDto.Id);
                        if (existing != null)
                        {
                            existing.SizeName = sizeDto.SizeName;
                            existing.Price = sizeDto.Price;
                        }
                    }
                    // Case 2: New size -> add it
                    else
                    {
                        product.Sizes.Add(new ProductSize
                        {
                            SizeName = sizeDto.SizeName,
                            Price = sizeDto.Price,
                            ProductId = product.Id
                        });
                    }
                }
                logger.LogInformation("updated");

                // // Case 3: Removed sizes -> delete them
                var incomingIds = request.Sizes
                    .Where(s => s.Id != 0)
                    .Select(s => s.Id)
                    .ToList();
                logger.LogInformation("updated ${i}", incomingIds.Count);
                
                
                var removed = product.Sizes
                    .Where(s => s.Id != 0 && !incomingIds.Contains(s.Id))
                    .ToList();
                logger.LogInformation("updated ${r}", removed.Count);
                
                
                dbContext.ProductSizes.RemoveRange(removed);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully updated product {ProductId}", request.Id);

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

                logger.LogInformation("Received request to update product {ProductId}", id);

                var result = await sender.Send(command);

                return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
            })
            .RequireAuthorization(PolicyConstants.ManagerOrAdminPolicy)
            .WithName("UpdateProduct")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Update a product with its sizes",
                Description = "Updates an existing product and replaces its size/price list. Requires admin privileges."
            });
    }
}
