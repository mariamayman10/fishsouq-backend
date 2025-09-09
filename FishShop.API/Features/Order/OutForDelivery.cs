using System.Security.Claims;
using Carter;
using FishShop.API.Database;
using FishShop.API.Entities.Enums;
using FishShop.API.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FishShop.API.Features;
public record OutForDeliveryCommand : IRequest<Result>
{
    public int Id { get; init; }
    public string UserId { get; init; }
}
public static class OutForDelivery
{
    public class Validator : AbstractValidator<OutForDeliveryCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(-1)
                .WithMessage("Invalid order ID");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<OutForDeliveryEndpoint> logger) : IRequestHandler<OutForDeliveryCommand, Result>
    {
        public async Task<Result> Handle(OutForDeliveryCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Marking order {OrderId} as OutForDelivery", request.Id);

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                return Result.BadRequest(validationResult.ToString());

            var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
            if (order == null)
                return Result.NotFound("Order not found");

            if (order.Status != OrderStatus.Confirmed)
                return Result.BadRequest("Order must be confirmed before it can be marked out for delivery");

            order.Status = OrderStatus.OutForDelivery;

            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}

public class OutForDeliveryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/orders/{orderId}/out-for-delivery", async (
            int orderId,
            ISender sender,
            ClaimsPrincipal user,
            ILogger<OutForDeliveryEndpoint> logger) =>
        {
            var command = new OutForDeliveryCommand { Id = orderId, UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) };
            var result = await sender.Send(command);
            return result.Resolve();
        }).RequireAuthorization(PolicyConstants.ManagerOrAdminPolicy);
    }
}
