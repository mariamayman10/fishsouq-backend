using Carter;
using FishShop.API.Database;
using FishShop.API.Shared;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Security.Claims;

namespace FishShop.API.Features;

public static class UpdateUserAddress
{
    public record Command : IRequest<Result>
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Street { get; set; }
        public string? Governorate { get; set; }
        public string? BuildingNumber { get; set; }
        public string? AptNumber { get; set; }
        public string? FloorNumber { get; set; }
        public bool IsDefault { get; set; }
        public string UserId { get; set; }
    }

    private class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(-1)
                .WithMessage("Invalid address ID");
            
            RuleFor(x => x.Name)
                .MaximumLength(LengthConstants.AddressName)
                .WithMessage($"Address name length can't exceed {LengthConstants.AddressName}");

            RuleFor(x => x.Street)
                .MaximumLength(LengthConstants.Street)
                .WithMessage($"Street length can't exceed {LengthConstants.Street}");

            RuleFor(x => x.Governorate)
                .MaximumLength(LengthConstants.Governorate)
                .WithMessage($"Governorate length can't exceed {LengthConstants.Governorate}");


            RuleFor(x => x.BuildingNumber)
                .MaximumLength(LengthConstants.BuildingNumber)
                .WithMessage($"Building Number length can't exceed {LengthConstants.BuildingNumber}");

            RuleFor(x => x.AptNumber)
                .MaximumLength(LengthConstants.AptNumber)
                .WithMessage($"Apartment number length can't exceed {LengthConstants.AptNumber}");

            RuleFor(x => x.FloorNumber)
                .MaximumLength(LengthConstants.FloorNumber)
                .WithMessage($"Floor number length can't exceed {LengthConstants.FloorNumber}");


            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User ID is required");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<UpdateUserAddressEndpoint> logger)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            using var scope = logger.BeginScope("AddressId: {AddressId}, UserId: {UserId}", 
                request.Id, request.UserId);
            logger.LogInformation("Attempting to update address");

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

            if (request.IsDefault)
            {
                var existingAddresses = await dbContext.UserAddresses
                    .Where(x => x.UserId == request.UserId && x.Id != request.Id)
                    .ToListAsync(cancellationToken);

                foreach (var existingAddress in existingAddresses)
                {
                    existingAddress.IsDefault = false;
                }
            }

            address.Street = request.Street?? address.Street;
            address.Governorate = request.Governorate?? address.Governorate;
            address.BuildingNumber = request.BuildingNumber?? address.BuildingNumber;
            address.AptNumber = request.AptNumber?? address.AptNumber;
            address.FloorNumber = request.FloorNumber?? address.FloorNumber;
            address.IsDefault = request.IsDefault;

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully updated address");

            return Result.Success();
        }
    }
}

public class UpdateUserAddressEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("api/addresses/{id}", async (
                int id,
                Contracts.UpdateUserAddress request,
                ISender sender,
                ILogger<UpdateUserAddressEndpoint> logger,
                ClaimsPrincipal user) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

                var command = request.Adapt<UpdateUserAddress.Command>();
                command.Id = id;
                command.UserId = userId;

                var result = await sender.Send(command);

                return result.Resolve();
            })
            .RequireAuthorization()
            .WithName("UpdateUserAddress")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Update an address",
                Description = "Updates an existing address for the authenticated user"
            });
    }
}