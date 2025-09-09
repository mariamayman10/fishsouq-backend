using System.Security.Claims;
using Carter;
using FishShop.API.Database;
using FishShop.API.Entities.Enums;
using FishShop.API.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FishShop.API.Features;

public record RejectOrderCommand : IRequest<Result>
{
    public int Id { get; init; }
    public string UserId { get; init; }
}
public static class RejectOrder
{
    public class Validator : AbstractValidator<RejectOrderCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(-1)
                .WithMessage("Invalid order ID");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<RejectOrderEndpoint> logger) : IRequestHandler<RejectOrderCommand, Result>
    {
        public async Task<Result> Handle(RejectOrderCommand request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Attempting to reject order {OrderId} for user {UserId}", request.Id, request.UserId);

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
                return Result.BadRequest(validationResult.ToString());

            var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
            if (order == null)
                return Result.NotFound("Order not found");

            if (order.Status != OrderStatus.Pending)
                return Result.BadRequest("Only pending orders can be rejected");

            order.Status = OrderStatus.Rejected;

            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}

public class RejectOrderEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/orders/{orderId}/reject", async (
            int orderId,
            ISender sender,
            ClaimsPrincipal user,
            ILogger<RejectOrderEndpoint> logger) =>
        {
            var command = new RejectOrderCommand { Id = orderId, UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) };
            var result = await sender.Send(command);
            return result.Resolve();
        }).RequireAuthorization(PolicyConstants.ManagerOrAdminPolicy);
    }
}
