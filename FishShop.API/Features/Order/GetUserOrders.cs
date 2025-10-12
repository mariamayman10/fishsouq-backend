using System.Security.Claims;
using Carter;
using FishShop.API.Contracts;
using FishShop.API.Database;
using FishShop.API.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FishShop.API.Features;

public static class GetUserOrders
{
    public record Query : IRequest<Result<PagedResult<OrderOverview>>>
    {
        public string? UserId { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
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
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<GetUserOrdersEndpoint> logger, IHttpContextAccessor httpContextAccessor)
        : IRequestHandler<Query, Result<PagedResult<OrderOverview>>>
    {
        public async Task<Result<PagedResult<OrderOverview>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                
                logger.LogWarning("User ID not found in claims");
                return Result.BadRequest<PagedResult<OrderOverview>>("Unauthorized access: user ID not found");
            }
            logger.LogInformation("Retrieving orders for UserId: {UserId}, Page: {Page}, PageSize: {PageSize}",
                request.UserId, request.Page, request.PageSize);

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogInformation("Invalid request, '{UserId}'", request.UserId);
                return Result.BadRequest<PagedResult<OrderOverview>>(validationResult.ToString());
            }


            if (string.IsNullOrEmpty(request.UserId))
            {
                logger.LogWarning("User ID is null or empty");
                return Result.BadRequest<PagedResult<OrderOverview>>("User ID is required");
            }

            if (request.Page < 1 || request.PageSize < 1)
            {
                logger.LogWarning("Invalid pagination parameters. Page: {Page}, PageSize: {PageSize}",
                    request.Page, request.PageSize);
                return Result.BadRequest<PagedResult<OrderOverview>>("Page and PageSize must be greater than 0");
            }

            var query = dbContext.Orders.Where(o => o.UserId == request.UserId);

            var totalItems = await query.CountAsync(cancellationToken);

            if (totalItems == 0)
            {
                logger.LogInformation("No orders found for UserId: {UserId}", request.UserId);
                return Result.Success(new PagedResult<OrderOverview>
                {
                    Items = new List<OrderOverview>(),
                    TotalItems = 0,
                    Page = request.Page,
                    PageSize = request.PageSize
                });
            }

            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(o => new OrderOverview
                {
                    Id = o.Id,
                    CreatedAt = o.CreatedAt,
                    TotalPrice = o.TotalPrice,
                    Status = o.Status,
                    DeliveryType = o.DeliveryType,
                    DeliveryAddress = o.AddressId,
                    Products = o.OrderProducts.Count
                })
                .ToListAsync(cancellationToken);

            return Result.Success(new PagedResult<OrderOverview>
            {
                Items = items,
                TotalItems = totalItems,
                Page = request.Page,
                PageSize = request.PageSize
            });
        }
    }
}

public class GetUserOrdersEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/users/orders", async (int page, int pageSize, ISender sender, HttpContext context) =>
        {
            var userId = context.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            var query = new GetUserOrders.Query { UserId = userId!, Page = page, PageSize = pageSize };
            var result = await sender.Send(query);
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}