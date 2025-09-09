using Carter;
using FishShop.API.Contracts;
using FishShop.API.Database;
using FishShop.API.Entities.Enums;
using FishShop.API.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FishShop.API.Features;

public static class GetProducts
{
    public record Query : IRequest<Result<PagedResult<Product>>>
    {
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int? CategoryId { get; init; }
        public OrderBy? OrderBy { get; init; }
        public string? SearchText { get; init; }
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

    internal sealed class Handler(AppDbContext dbContext, ILogger<GetProductsEndpoint> logger)
        : IRequestHandler<Query, Result<PagedResult<Product>>>
    {
        public async Task<Result<PagedResult<Product>>> Handle(Query request,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Handling GetProducts request for page {Page} with page size {PageSize}",
                request.Page, request.PageSize);

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogInformation("Invalid request");
                return Result.BadRequest<PagedResult<Product>>(validationResult.ToString());
            }

            var pagedResult = string.IsNullOrEmpty(request.SearchText)
                ? await HandleWithoutSearch(request, cancellationToken)
                : await HandleWithSearch(request, cancellationToken);

            logger.LogInformation("Retrieved {PageSize} products for page {Page}", pagedResult.PageSize,
                request.Page);

            return Result.Success(pagedResult);
        }
// on search while choosing sort from high to low, the result of search is not sorted
        private async Task<PagedResult<Product>> HandleWithSearch(Query request, CancellationToken cancellationToken)
        {
            var orderByString = request.OrderBy switch
            {
                OrderBy.PriceAsc => @", p.""Price"" ASC",
                OrderBy.PriceDesc => @", p.""Price"" DESC",
                OrderBy.SalesAsc => @", ps.""TotalQuantitySold"" ASC",
                OrderBy.SalesDesc or null => @", ps.""TotalQuantitySold"" DESC",
                _ => throw new ArgumentOutOfRangeException()
            };

            object[] queryParams =
            [
                new NpgsqlParameter("searchTerm", request.SearchText),
                new NpgsqlParameter("pageSize", request.PageSize),
                new NpgsqlParameter("pageNumber", request.Page - 1)
            ];

            var categoryCondition =
                request.CategoryId.HasValue ? $"AND \"CategoryId\" = {request.CategoryId}" : string.Empty;

            var countQuery = $"""
                              SELECT COUNT(1) AS "Value"
                              FROM "Products" AS p
                              LEFT JOIN "ProductSales" AS ps ON p."Id" = ps."ProductId"
                              WHERE p."Quantity" > 0 AND p."IsDeleted" = FALSE
                                {categoryCondition}
                                AND (similarity(p."Name", @searchTerm) >= 0.3 OR p."Name" ILIKE '%' || @searchTerm || '%')
                              """;

            var totalItems = await dbContext.Database.SqlQueryRaw<int>(countQuery, queryParams)
                .FirstAsync(cancellationToken);

            var rawQuery = $"""
                            SELECT p."Id", p."Quantity", p."Name", p."Price", p."Description", p."ImageUrl"
                            FROM "Products" AS p
                            LEFT JOIN "ProductSales" AS ps ON p."Id" = ps."ProductId"
                            WHERE p."Quantity" > 0 AND p."IsDeleted" = FALSE
                              {categoryCondition}
                              AND (similarity(p."Name", @searchTerm) >= 0.3 OR p."Name" ILIKE '%' || @searchTerm || '%')
                            ORDER BY similarity(p."Name", @searchTerm) DESC {orderByString}
                            LIMIT @PageSize OFFSET @PageNumber * @PageSize
                            """;

            var query = dbContext.Database.SqlQueryRaw<Product>(rawQuery,queryParams).IgnoreQueryFilters();

            var items = await query
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<Product>
            {
                Items = items,
                TotalItems = totalItems,
                Page = request.Page,
                PageSize = request.PageSize
            };

            return pagedResult;
        }

        private async Task<PagedResult<Product>> HandleWithoutSearch(Query request, CancellationToken cancellationToken)
        {
            var query = dbContext.Products.Where(p => p.Quantity > 0);

            if (request.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == request.CategoryId.Value);

            var totalItems = await query.CountAsync(cancellationToken);

            // query = request.OrderBy switch
            // {
            //     OrderBy.PriceAsc => query.OrderBy(p => p.Price).ThenBy(p => p.Id),
            //     OrderBy.PriceDesc => query.OrderByDescending(p => p.Price).ThenBy(p => p.Id),
            //     OrderBy.SalesAsc => query.OrderBy(p => p.ProductSales!.TotalQuantitySold).ThenBy(p => p.Id),
            //     OrderBy.SalesDesc or null => query.OrderByDescending(p => p.ProductSales!.TotalQuantitySold)
            //         .ThenBy(p => p.Id),
            //     _ => throw new ArgumentOutOfRangeException()
            // };
            query = request.OrderBy switch
            {
                OrderBy.PriceAsc => query.OrderBy(p => p.Price),
                OrderBy.PriceDesc => query.OrderByDescending(p => p.Price),
                OrderBy.SalesAsc => query.OrderBy(p => p.ProductSales != null ? p.ProductSales.TotalQuantitySold : 0),
                OrderBy.SalesDesc or null => query.OrderByDescending(p => p.ProductSales != null ? p.ProductSales.TotalQuantitySold : 0),
                _ => throw new ArgumentOutOfRangeException()
            };

            var items = await query
                .AsNoTracking()
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new Product
                {
                    Id = p.Id,
                    Name = p.Name!,
                    Price = p.Price,
                    Quantity = p.Quantity,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl
                })
                .ToListAsync(cancellationToken);
            logger.LogInformation("Fetched {items} products", items.Count);
            var pagedResult = new PagedResult<Product>
            {
                Items = items,
                TotalItems = totalItems,
                Page = request.Page,
                PageSize = request.PageSize
            };

            return pagedResult;
        }
    }
}

public class GetProductsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/products", async (
            int page,
            int pageSize,
            int? categoryId,
            string? searchText,
            OrderBy? orderBy,
            ISender sender) =>
        {
            var query = new GetProducts.Query
            {
                Page = page,
                PageSize = pageSize,
                CategoryId = categoryId,
                OrderBy = orderBy,
                SearchText = searchText
            };
            var result = await sender.Send(query);
            return result.Resolve();
        }).RequireRateLimiting(RateLimitPolicyConstants.Sliding);
    }
}