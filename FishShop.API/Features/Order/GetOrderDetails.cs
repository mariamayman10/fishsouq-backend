using System.Security.Claims;
using Carter;
using FishShop.API.Contracts;
using FishShop.API.Database;
using FishShop.API.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FishShop.API.Features;

public static class GetOrderDetails
{
    public record Query : IRequest<Result<OrderDetails>>
    {
        public int Id { get; init; }
        public ClaimsPrincipal User { get; init; } = default!;
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(-1)
                .WithMessage("Invalid order ID");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<GetOrderDetailsEndpoint> logger)
        : IRequestHandler<Query, Result<OrderDetails>>
    {
        public async Task<Result<OrderDetails>> Handle(Query request, CancellationToken cancellationToken)
        {
            var userId = request.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdminOrManager = request.User.FindFirstValue(ClaimTypes.Role) == "AdminRole" || request.User.FindFirstValue(ClaimTypes.Role) == "ManagerRole";

            logger.LogInformation("Retrieving order details for OrderId: {OrderId}, RequestedBy: {UserId}, IsAdmin: {IsAdmin}",
                request.Id, userId, isAdminOrManager);

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogInformation("Invalid request, '{OrderId}'", request.Id);
                return Result.BadRequest<OrderDetails>(validationResult.ToString());
            }

            var order = await dbContext.Orders
                .Where(o => o.Id == request.Id)
                .Include(o => o.OrderProducts!)
                    .ThenInclude(op => op.ProductSize)
                .Include(o => o.User)
                .FirstOrDefaultAsync(cancellationToken);

            if (order == null)
            {
                logger.LogWarning("Order not found for OrderId: {OrderId}", request.Id);
                return Result.NotFound<OrderDetails>("No order with the provided Id");
            }

            if (order.UserId != userId && !isAdminOrManager)
            {
                logger.LogWarning("Unauthorized access attempt to OrderId: {OrderId} by UserId: {UserId}", request.Id, userId);
                return Result.NotFound<OrderDetails>("You are not authorized to access this order");
            }

            var orderDetails = new OrderDetails
            {
                Id = order.Id,
                Address = order.Address,
                TotalAmount = order.OrderProducts.Sum(p => p.UnitPrice * p.Quantity),
                UserName = order.User?.UserName ?? "",
                UserId = order.UserId,
                CreatedAt = order.CreatedAt,
                DeliveryDate = order.DeliveryDate,
                DeliveryFees = order.DeliveryFees,
                Status = order.Status,
                PaymentInfo = order.PaymentInfo,
                Discount = order.Discount,
                PromoCode = order.PromoCode,
                Items = order.OrderProducts.Select(op => new OrderItem
                {
                    ProductId = op.ProductSize.ProductId,
                    SizeName = op.ProductSize.SizeName,
                    Quantity = op.Quantity,
                    UnitPrice = op.UnitPrice
                }).ToList()
            };

            logger.LogInformation("Successfully retrieved order details for OrderId: {OrderId}", request.Id);
            return Result.Success(orderDetails);
        }
    }
}

public class GetOrderDetailsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/orders/{orderId}", async (
            int orderId,
            ClaimsPrincipal user,
            ISender sender,
            ILogger<GetOrderDetailsEndpoint> logger) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("User ID not found in claims");
                return Results.Unauthorized();
            }
            var query = new GetOrderDetails.Query
            {
                Id = orderId,
                User = user
            };

            var result = await sender.Send(query);
            return result.Resolve();
        }).RequireAuthorization();
    }
}
