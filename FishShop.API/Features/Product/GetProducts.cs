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

        private async Task<PagedResult<Product>> HandleWithSearch(Query request, CancellationToken cancellationToken)
        {
            // guard: treat page <= 0 as page 1 to prevent negative skip
            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

            // base query
            var q = dbContext.Products
                .Where(p => p.Quantity > 0 && !p.IsDeleted);

            if (request.CategoryId.HasValue)
                q = q.Where(p => p.CategoryId == request.CategoryId.Value);

            // apply search (case-insensitive, Postgres ILIKE)
            var search = request.SearchText?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                // search in name OR description
                q = q.Where(p =>
                    EF.Functions.ILike(p.Name!, $"%{search}%")
                    || EF.Functions.ILike(p.Description!, $"%{search}%"));
            }

            // total count
            var totalItems = await q.CountAsync(cancellationToken);

            // fetch paged items with sizes projected
            var items = await q
                .AsNoTracking()
                .OrderBy(p => p.Id) // stable ordering for pagination
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new Product
                {
                    Id = p.Id,
                    Name = p.Name!,
                    Quantity = p.Quantity,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    Sizes = p.Sizes.Select(s => new ProductSizeDto
                    {
                        Id = s.Id,
                        SizeName = s.SizeName,
                        Price = s.Price
                    }).ToList()
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<Product>
            {
                Items = items,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize
            };
        }


        private async Task<PagedResult<Product>> HandleWithoutSearch(Query request, CancellationToken cancellationToken)
        {
            var query = dbContext.Products
                .Where(p => p.Quantity > 0 && !p.IsDeleted);

            if (request.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == request.CategoryId.Value);
            query.Include(p => p.Sizes);

            var totalItems = await query.CountAsync(cancellationToken);

            var items = await query
                .AsNoTracking()
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new Product
                {
                    Id = p.Id,
                    Name = p.Name!,
                    Quantity = p.Quantity,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    Sizes = p.Sizes
                        .Select(s => new ProductSizeDto
                        {
                            Id = s.Id,
                            SizeName = s.SizeName,
                            Price = s.Price
                        })
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            logger.LogInformation("Fetched {items} products", items.Count);

            return new PagedResult<Product>
            {
                Items = items,
                TotalItems = totalItems,
                Page = request.Page,
                PageSize = request.PageSize
            };
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
            ISender sender) =>
        {
            var query = new GetProducts.Query
            {
                Page = page,
                PageSize = pageSize,
                CategoryId = categoryId,
                SearchText = searchText
            };
            var result = await sender.Send(query);
            return result.Resolve();
        }).RequireRateLimiting(RateLimitPolicyConstants.Sliding);
    }
}