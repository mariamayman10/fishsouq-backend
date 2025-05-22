using Carter;
using FishShop.API.Database;
using FishShop.API.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Security.Claims;

namespace FishShop.API.Features;

public static class DeleteUserAddress
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
                .WithMessage("Invalid address ID");

            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<DeleteUserAddressEndpoint> logger)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            using var scope = logger.BeginScope("AddressId: {AddressId}, UserId: {UserId}", 
                request.Id, request.UserId);
            logger.LogInformation("Attempting to delete address");

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogInformation("Invalid request");
                return Result.BadRequest(validationResult.ToString());
            }

            var address = await dbContext.UserAddresses
                .FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == request.UserId, 
                    cancellationToken);

            if (address == null)
            {
                logger.LogWarning("Address not found");
                return Result.NotFound("Address not found");
            }

            dbContext.UserAddresses.Remove(address);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully deleted address");

            return Result.Success();
        }
    }
}

public class DeleteUserAddressEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/addresses/{id}", async (
                int id,
                ISender sender,
                ILogger<DeleteUserAddressEndpoint> logger,
                ClaimsPrincipal user) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }

                var command = new DeleteUserAddress.Command { Id = id, UserId = userId };
                var result = await sender.Send(command);

                return result.Resolve();
            })
            .RequireAuthorization()
            .WithName("DeleteUserAddress")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Delete an address",
                Description = "Deletes an existing address for the authenticated user"
            });
    }
}