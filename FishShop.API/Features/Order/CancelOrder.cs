using System.Security.Claims;
using Carter;
using FishShop.API.Database;
using FishShop.API.Entities.Enums;
using FishShop.API.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Logging;
using Microsoft.OpenApi.Models;

namespace FishShop.API.Features;

public static class CancelOrder
{
    public record Command : IRequest<Result>
    {
        public int Id { get; init; }
        public string? UserId { get; init; }
    }

    private class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(-1)
                .WithMessage("Invalid order ID");
            
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<CancelOrderEndpoint> logger)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            using var scope = logger.BeginScope("Order Id {OrderId}", request.Id);

            logger.LogInformation("Attempting to cancel order for user {UserId}", request.UserId);

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogInformation("Invalid request");
                return Result.BadRequest(validationResult.ToString());
            }

            var order = await dbContext.Orders
                .Where(o => o.Id == request.Id && o.UserId == request.UserId)
                .FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);

            if (order == null)
            {
                logger.LogWarning("Order not found");
                return Result.NotFound("Order not found");
            }

            if (order.Status != OrderStatus.Pending)
            {
                logger.LogWarning("Cannot cancel order in status {Status}", order.Status);
                return Result.BadRequest("Order cannot be cancelled in its current status");
            }

            var originalStatus = order.Status;
            order.Status = OrderStatus.Cancelled;

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Successfully cancelled order. Status changed from {OldStatus} to {NewStatus}",
                originalStatus, OrderStatus.Cancelled);

            return Result.Success();
        }
    }
}

public class CancelOrderEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/orders/{id}/cancel", async (
                int id,
                ISender sender,
                ILogger<CancelOrderEndpoint> logger,
                ClaimsPrincipal user) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                logger.LogInformation("User {UserId} attempting to cancel order {OrderId}", userId, id);

                var command = new CancelOrder.Command { Id = id, UserId = userId };
                var result = await sender.Send(command);

                return result.Resolve();
            }).RequireAuthorization()
            .WithName("CancelOrder")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Cancel an order",
                Description = "Cancels a pending order. Only the order owner can cancel their order."
            });
    }
}