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

public static class GetAllProducts
{
    public record Query : IRequest<Result<PagedResult<ProductDto>>>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int? CategoryId { get; set; }
        public OrderBy? OrderBy { get; set; }
        public string? SearchText { get; set; }
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
        : IRequestHandler<Query, Result<PagedResult<ProductDto>>>
    {
        public async Task<Result<PagedResult<ProductDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Handling GetAllProducts request for page {Page} with page size {PageSize}",
                request.Page, request.PageSize);

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogInformation("Invalid request");
                return Result.BadRequest<PagedResult<ProductDto>>(validationResult.ToString());
            }

            var pagedResult = string.IsNullOrEmpty(request.SearchText)
                ? await HandleWithoutSearch(request, cancellationToken)
                : await HandleWithSearch(request, cancellationToken);

            return Result.Success(pagedResult);
        }

        private async Task<PagedResult<ProductDto>> HandleWithSearch(Query request, CancellationToken cancellationToken)
        {
            var orderByString = request.OrderBy switch
            {
                OrderBy.PriceAsc => "ORDER BY MIN(s.\"Price\") ASC",
                OrderBy.PriceDesc => "ORDER BY MAX(s.\"Price\") DESC",
                OrderBy.SalesAsc => "ORDER BY ps.\"TotalQuantitySold\" ASC",
                OrderBy.SalesDesc or null => "ORDER BY ps.\"TotalQuantitySold\" DESC",
                OrderBy.AvQuantityAsc => "ORDER BY p.\"Quantity\" ASC",
                OrderBy.AvQuantityDesc => "ORDER BY p.\"Quantity\" DESC",
                _ => "ORDER BY similarity(p.\"Name\", @searchTerm) DESC"
            };

            var categoryCondition = request.CategoryId.HasValue
                ? $"AND p.\"CategoryId\" = {request.CategoryId}"
                : string.Empty;

            object[] queryParams =
            [
                new NpgsqlParameter("searchTerm", request.SearchText ?? string.Empty),
                new NpgsqlParameter("pageSize", request.PageSize),
                new NpgsqlParameter("pageNumber", request.Page - 1)
            ];

            // Count total items for pagination
            var countQuery = $"""
                                  SELECT COUNT(DISTINCT p."Id") AS "Value"
                                  FROM "Products" p
                                  WHERE p."IsDeleted" = FALSE
                                  {categoryCondition}
                                  AND (
                                      similarity(p."Name", @searchTerm) >= 0.3 
                                      OR p."Name" ILIKE '%' || @searchTerm || '%'
                                  )
                              """;

            var totalItems = await dbContext.Database
                .SqlQueryRaw<int>(countQuery, queryParams)
                .FirstAsync(cancellationToken);

            // Flattened query with join for sizes
            var rawQuery = $"""
                                SELECT 
                                    p."Id" AS "ProductId",
                                    p."Name",
                                    p."Description",
                                    p."ImageUrl",
                                    p."Quantity",
                                    COALESCE(ps."TotalQuantitySold", 0) AS "TotalQuantitySold",
                                    COALESCE(ps."TotalRevenue", 0) AS "TotalRevenue",
                                    s."SizeName",
                                    s."Price"
                                FROM "Products" p
                                LEFT JOIN "ProductSales" ps ON ps."ProductId" = p."Id"
                                LEFT JOIN "ProductSizes" s ON s."ProductId" = p."Id"
                                WHERE p."IsDeleted" = FALSE
                                {categoryCondition}
                                AND (
                                    similarity(p."Name", @searchTerm) >= 0.3 
                                    OR p."Name" ILIKE '%' || @searchTerm || '%'
                                )
                                {orderByString}
                                LIMIT @pageSize OFFSET @pageNumber * @pageSize;
                            """;

            // Execute SQL
            var flatList = await dbContext.Database
                .SqlQueryRaw<ProductFlatDto>(rawQuery, queryParams)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            // Group results by ProductId
            var products = flatList
                .GroupBy(p => p.ProductId)
                .Select(g => new ProductDto
                {
                    Id = g.Key,
                    Name = g.First().Name,
                    Description = g.First().Description,
                    ImageUrl = g.First().ImageUrl,
                    Quantity = g.First().Quantity,
                    TotalRevenue = g.First().TotalRevenue,
                    TotalQuantitySold = g.First().TotalQuantitySold,
                    Sizes = g
                        .Where(x => x.SizeName != null)
                        .Select(x => new ProductSizeDto
                        {
                            SizeName = x.SizeName!,
                            Price = x.Price ?? 0
                        })
                        .ToList()
                })
                .ToList();

            return new PagedResult<ProductDto>
            {
                Items = products,
                TotalItems = totalItems,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        private async Task<PagedResult<ProductDto>> HandleWithoutSearch(Query request,
            CancellationToken cancellationToken)
        {
            var query = dbContext.Products.Include(p => p.ProductSales).AsQueryable();

            if (request.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == request.CategoryId.Value);

            var totalItems = await query.CountAsync(cancellationToken);

            query = request.OrderBy switch
            {
                OrderBy.SalesAsc => query.OrderBy(p => p.ProductSales != null ? p.ProductSales.TotalQuantitySold : 0),
                OrderBy.SalesDesc or null => query.OrderByDescending(p =>
                    p.ProductSales != null ? p.ProductSales.TotalQuantitySold : 0),
                _ => throw new ArgumentOutOfRangeException()
            };

            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name ?? "",
                    Sizes = p.Sizes
                        .Select(s => new ProductSizeDto
                        {
                            SizeName = s.SizeName,
                            Price = s.Price
                        })
                        .ToList(),
                    Quantity = p.Quantity,
                    Description = p.Description ?? "",
                    ImageUrl = p.ImageUrl ?? "",
                    TotalQuantitySold = p.ProductSales != null ? p.ProductSales.TotalQuantitySold : 0,
                    TotalRevenue = p.ProductSales != null ? p.ProductSales.TotalRevenue : 0
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<ProductDto>
            {
                Items = items,
                TotalItems = totalItems,
                Page = request.Page,
                PageSize = request.PageSize
            };
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
                string? searchText,
                ISender sender) =>
            {
                var query = new GetAllProducts.Query
                {
                    Page = page,
                    PageSize = pageSize,
                    CategoryId = categoryId,
                    OrderBy = orderBy,
                    SearchText = searchText
                };
                var result = await sender.Send(query);
                return result.Resolve();
            })
            .RequireAuthorization(PolicyConstants.ManagerOrAdminPolicy);
    }
}