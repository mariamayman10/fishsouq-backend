using System.Security.Claims;
using Carter;
using FishShop.API.Database;
using FishShop.API.Entities;
using FishShop.API.Entities.Enums;
using FishShop.API.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FishShop.API.Features;

public record MarkDeliveredCommand : IRequest<Result>
{
    public int Id { get; init; }
    public string UserId { get; init; }
}
public static class MarkDelivered
{
    public class Validator : AbstractValidator<MarkDeliveredCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(-1)
                .WithMessage("Invalid order ID");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<DeliveredEndpoint> logger) : IRequestHandler<MarkDeliveredCommand, Result>
    {
        public async Task<Result> Handle(MarkDeliveredCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Marking order {OrderId} as Delivered", request.Id);

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                return Result.BadRequest(validationResult.ToString());

            var order = await dbContext.Orders
                .Include(o => o.Products)!
                .ThenInclude(op => op.Product)
                .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

            if (order == null)
                return Result.NotFound("Order not found");

            if (order.Status != OrderStatus.OutForDelivery && order.Status != OrderStatus.AwaitingCustomer)
                return Result.BadRequest("Order must be out for delivery or awaiting customer before being marked delivered");

            order.Status = OrderStatus.Delivered;
            order.DeliveryDate = DateTime.UtcNow;

            // Update ProductSales
            foreach (var orderProduct in order.Products ?? Enumerable.Empty<OrderProduct>())
            {
                if (orderProduct.ProductId == 0 || orderProduct.Quantity == 0) continue;

                var productSales = await dbContext.Set<ProductSales>()
                    .FirstOrDefaultAsync(ps => ps.ProductId == orderProduct.ProductId, cancellationToken);

                if (productSales == null)
                {
                    productSales = new ProductSales
                    {
                        ProductId = orderProduct.ProductId,
                        TotalQuantitySold = orderProduct.Quantity,
                        TotalRevenue = orderProduct.Quantity * orderProduct.UnitPrice
                    };
                    await dbContext.AddAsync(productSales, cancellationToken);
                }
                else
                {
                    productSales.TotalQuantitySold += orderProduct.Quantity;
                    productSales.TotalRevenue += orderProduct.Quantity * orderProduct.UnitPrice;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}

public class DeliveredEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/orders/{orderId}/delivered", async (
            int orderId,
            ISender sender,
            ClaimsPrincipal user,
            ILogger<DeliveredEndpoint> logger) =>
        {
            var command = new MarkDeliveredCommand
                { Id = orderId, UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) };
            var result = await sender.Send(command);
            return result.Resolve();
        }).RequireAuthorization(PolicyConstants.ManagerOrAdminPolicy);
    }
}
