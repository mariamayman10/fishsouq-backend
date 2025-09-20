using System.Security.Claims;
using Carter;
using FishShop.API.Database;
using FishShop.API.Entities.Enums;
using FishShop.API.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FishShop.API.Features;
public record Command : IRequest<Result>
{
    public int Id { get; init; }
    public string UserId { get; init; }
}
public static class ConfirmOrder
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(-1)
                .WithMessage("Invalid order ID");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<ConfirmOrderEndpoint> logger) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Attempting to confirm order {OrderId} for user {UserId}",
                request.Id, request.UserId);


            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogInformation("Invalid request, '{OrderId}'", request.Id);
                return Result.BadRequest(validationResult.ToString());
            }


            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var order = await dbContext.Orders
                .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

            if (order == null)
            {
                logger.LogWarning("Order {OrderId} not found", request.Id);
                return Result.NotFound( "Order not found");
            }

            if (order.Status != OrderStatus.Pending)
            {
                logger.LogWarning("Invalid order status transition zz from {CurrentStatus} for order {OrderId}",
                    order.Status, request.Id);
                return Result.BadRequest("Order cannot be confirmed from its current status");
            }

            order.Status = order.DeliveryType == DeliveryType.HomeDelivery
                ? OrderStatus.Confirmed
                : OrderStatus.AwaitingCustomer;

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation("Successfully confirmed order {OrderId} with status {Status}",
                request.Id, order.Status);
            return Result.Success();
        }
    }

    
}
public class ConfirmOrderEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/orders/{orderId}/confirm", async (
            int orderId,
            ISender sender,
            ClaimsPrincipal user,
            ILogger<ConfirmOrderEndpoint> logger) =>
        {

            var command = new Command { Id = orderId, UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) };
            var result = await sender.Send(command);

            return result.Resolve();
        }).RequireAuthorization(PolicyConstants.ManagerOrAdminPolicy);
    }
}