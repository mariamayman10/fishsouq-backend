using Carter;
using FishShop.API.Contracts;
using FishShop.API.Database;
using FishShop.API.Entities.Enums;
using FishShop.API.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FishShop.API.Features;

public static class GetAllProducts
{
    public record Query : IRequest<Result<PagedResult<Product>>>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int? CategoryId { get; set; }
        public OrderBy? OrderBy { get; set; }
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(-1)
                .WithMessage("Invalid page number");

            RuleFor(x => x.PageSize)
                .GreaterThan(-1)
                .WithMessage("Invalid page size");

            RuleFor(x => x.CategoryId)
                .GreaterThan(-1)
                .WithMessage("Invalid category Id");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<GetAllProductsEndpoint> logger)
        : IRequestHandler<Query, Result<PagedResult<Product>>>
    {
        public async Task<Result<PagedResult<Product>>> Handle(Query request,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Handling GetAllProducts request for page {Page} with page size {PageSize}",
                request.Page, request.PageSize);

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogInformation("Invalid request");
                return Result.BadRequest<PagedResult<Product>>(validationResult.ToString());
            }

            var query = dbContext.Products.AsQueryable();

            if (request.CategoryId.HasValue) query = query.Where(p => p.CategoryId == request.CategoryId.Value);

            query = request.OrderBy switch
            {
                OrderBy.PriceAsc => query.OrderBy(p => p.Price),
                OrderBy.PriceDesc => query.OrderByDescending(p => p.Price),
                OrderBy.SalesAsc => query.OrderBy(p => p.ProductSales),
                OrderBy.SalesDesc or null => query.OrderByDescending(p => p.ProductSales),
                _ => throw new ArgumentOutOfRangeException()
            };

            var totalItems = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new Product
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price
                })
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<Product>
            {
                Items = items,
                TotalItems = totalItems,
                Page = request.Page,
                PageSize = request.PageSize
            };

            logger.LogInformation("Retrieved {TotalItems} products for page {Page}", totalItems, request.Page);

            return Result.Success(pagedResult);
        }
    }
}

public class GetAllProductsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/products/all", async (
                int page,
                int pageSize,
                int? categoryId,
                OrderBy? orderBy,
                ISender sender) =>
            {
                var query = new GetProducts.Query
                {
                    Page = page,
                    PageSize = pageSize,
                    CategoryId = categoryId,
                    OrderBy = orderBy
                };
                var result = await sender.Send(query);
                return result.Resolve();
            })
            .RequireAuthorization(PolicyConstants.AdminPolicy);
    }
}