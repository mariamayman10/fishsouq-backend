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
        public string? UserId { get; init; }
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
            logger.LogInformation("Retrieving order details for OrderId: {OrderId}, UserId: {UserId}",
                request.Id, request.UserId);


            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogInformation("Invalid request, '{OrderId}'", request.Id);
                return Result.BadRequest<OrderDetails>(validationResult.ToString());
            }


            var orderProducts = await dbContext.Orders
                .Where(o => o.Id == request.Id && o.UserId == request.UserId)
                .SelectMany(o => o.Products!)
                .Select(o=> new
                {
                    ProductName = o.Product!.Name,
                    o.ProductId,
                    o.Quantity,
                    o.UnitPrice,
                    o.Order!.User!.UserName,
                    o.Order.AddressId,
                    o.Order.CreatedAt,
                    o.Order.Status
                })
                .ToListAsync(cancellationToken);

            if (orderProducts.Count == 0)
            {
                logger.LogWarning("Order not found for OrderId: {OrderId}", request.Id);
                return Result.NotFound<OrderDetails>("No order with the provided Id");
            }

            var orderDetails = new OrderDetails
            {
                Id = request.Id,
                AddressId = orderProducts.First().AddressId,
                TotalAmount = orderProducts.Sum(o => o.UnitPrice * o.Quantity),
                UserName = orderProducts.First().UserName!,
                CreatedAt = orderProducts.First().CreatedAt,
                Status = orderProducts.First().Status,
                Items = orderProducts.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
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
            ISender sender,
            ILogger<GetOrderDetailsEndpoint> logger,
            ClaimsPrincipal user) =>
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
                UserId = userId
            };

            var result = await sender.Send(query);

            return result.Resolve();
        }).RequireAuthorization();
    }
}