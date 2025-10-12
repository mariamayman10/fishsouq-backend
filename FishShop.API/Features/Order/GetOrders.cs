using System.Data;
using System.Security.Claims;
using Carter;
using FishShop.API.Contracts;
using FishShop.API.Database;
using FishShop.API.Entities.Enums;
using FishShop.API.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace FishShop.API.Features;

public static class GetOrders
{
    public record Query : IRequest<Result<PagedResult<OrderOverview>>>
    {
        public DateTime? FromCreatedAt { get; set; }
        public DateTime? ToCreatedAt { get; set; }
        public DateTime? FromDeliveryDate { get; set; }
        public DateTime? ToDeliveryDate { get; set; }
        public DeliveryType? DeliveryType { get; set; }
        public OrderStatus? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page)
                .GreaterThan(-1)
                .WithMessage("Page must be greater than 0");

            RuleFor(x => x.PageSize)
                .GreaterThan(-1)
                .LessThanOrEqualTo(100)
                .WithMessage("Page size must be between 1 and 100");

            When(x => x.FromCreatedAt.HasValue && x.ToCreatedAt.HasValue, () =>
            {
                RuleFor(x => x.ToCreatedAt)
                    .GreaterThanOrEqualTo(x => x.FromCreatedAt)
                    .WithMessage("ToCreatedAt must be after FromCreatedAt");
            });

            When(x => x.FromDeliveryDate.HasValue && x.ToDeliveryDate.HasValue, () =>
            {
                RuleFor(x => x.ToDeliveryDate)
                    .GreaterThanOrEqualTo(x => x.FromDeliveryDate)
                    .WithMessage("ToDeliveryDate must be after FromDeliveryDate");
            });
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<GetOrdersEndpoint> logger)
        : IRequestHandler<Query, Result<PagedResult<OrderOverview>>>
    {
        public async Task<Result<PagedResult<OrderOverview>>> Handle(Query request, CancellationToken cancellationToken)
        {
            logger.LogInformation(
                "Fetching orders with filters: {@Filters}",
                new
                {
                    request.FromCreatedAt,
                    request.ToCreatedAt,
                    request.FromDeliveryDate,
                    request.ToDeliveryDate,
                    request.DeliveryType,
                    request.Status,
                    request.Page,
                    request.PageSize
                });

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogInformation("Invalid request");
                return Result.BadRequest<PagedResult<OrderOverview>>(validationResult.ToString());
            }

            // var transaction = await dbContext.Database.GetDbConnection()
            //     .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            // var connection = dbContext.Database.GetDbConnection();
            // if (connection.State != ConnectionState.Open)
            //     await connection.OpenAsync(cancellationToken);
            //
            // var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);


            var query = dbContext.Orders.Include(o => o.User).AsQueryable();

            // Apply filters
            if (request.FromCreatedAt.HasValue)
                query = query.Where(x => x.CreatedAt >= request.FromCreatedAt.Value);

            if (request.ToCreatedAt.HasValue)
                query = query.Where(x => x.CreatedAt <= request.ToCreatedAt.Value);

            if (request.FromDeliveryDate.HasValue)
                query = query.Where(x => x.DeliveryDate >= request.FromDeliveryDate.Value);

            if (request.ToDeliveryDate.HasValue)
                query = query.Where(x => x.DeliveryDate <= request.ToDeliveryDate.Value);

            if (request.DeliveryType.HasValue)
                query = query.Where(x => x.DeliveryType == request.DeliveryType.Value);

            if (request.Status.HasValue)
                query = query.Where(x => x.Status == request.Status.Value);

            query = query.OrderByDescending(x => x.CreatedAt);

            var totalItems = await query.CountAsync(cancellationToken);

            if (totalItems == 0)
            {
                logger.LogInformation("No orders found matching the criteria");
                return Result.Success(new PagedResult<OrderOverview>
                {
                    Items = new List<OrderOverview>(),
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalItems = 0
                });
            }

            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(o => new OrderOverview
                {
                    Id = o.Id,
                    CreatedAt = o.CreatedAt,
                    DeliveryDate = o.DeliveryDate,
                    DeliveryType = o.DeliveryType,
                    Status = o.Status,
                    TotalPrice = o.TotalPrice,
                    UserName = o.User.UserName,
                    Products = o.OrderProducts.Count
                })
                .ToListAsync(cancellationToken);


            logger.LogInformation(
                "Successfully retrieved {Count} orders out of {Total} total",
                items.Count,
                totalItems);

            return Result.Success(new PagedResult<OrderOverview>
            {
                Items = items,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = totalItems
            });
        }
    }
}

public class GetOrdersEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/orders", async (
                [AsParameters] GetOrders.Query query,
                ISender sender,
                ILogger<GetOrdersEndpoint> logger,
                ClaimsPrincipal user) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                logger.LogInformation("User {UserId} requesting orders", userId);

                var result = await sender.Send(query);

                return result.Resolve();
            })
            .RequireAuthorization(PolicyConstants.ManagerOrAdminPolicy)
            .WithName("GetOrders")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Get paginated orders",
                Description = "Retrieves a paginated list of orders with optional filtering"
            });
    }
}