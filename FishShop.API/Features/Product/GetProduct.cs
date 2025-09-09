using Carter;
using FishShop.API.Contracts;
using FishShop.API.Database;
using FishShop.API.Shared;
using FluentValidation;
using MediatR;

namespace FishShop.API.Features;

public static class GetProduct
{
    public record Query : IRequest<Result<ProductDetails>>
    {
        public int Id { get; set; }
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(-1)
                .WithMessage("Invalid product Id");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<GetProductEndpoint> logger)
        : IRequestHandler<Query, Result<ProductDetails>>
    {
        public async Task<Result<ProductDetails>> Handle(Query request,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Handling GetProduct request for product {ProductId}",
                request.Id);

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogInformation("Invalid request");
                return Result.BadRequest<ProductDetails>(validationResult.ToString());
            }

            var product = dbContext.Products.Where(p => p.Id == request.Id)
                .Select(p => new ProductDetails
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Quantity = p.Quantity,
                    CategoryId = p.CategoryId,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl
                }).SingleOrDefault();

            if (product is null)
                return Result.NotFound<ProductDetails>("An error occurred while retrieving products, product not found.");

            logger.LogInformation("Product retrieved");

            return Result.Success(product);
        }
    }
}

public class GetProductEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/products/{id}", async (
                int id,
                ISender sender) =>
            {
                var query = new GetProduct.Query
                {
                    Id = id
                };
                var result = await sender.Send(query);

                return result.Resolve();
            });
    }
}