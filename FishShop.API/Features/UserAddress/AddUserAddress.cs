using Carter;
using FishShop.API.Database;
using FishShop.API.Entities;
using FishShop.API.Shared;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Security.Claims;

namespace FishShop.API.Features
{
    public static class AddUserAddress
    {
        public record Command : IRequest<Result<int>>
        {
            public string Name { get; init; }
            public string Street { get; set; }
            public string Governorate { get; set; }
            public string BuildingNumber { get; set; }
            public string AptNumber { get; set; }
            public string FloorNumber { get; set; }
            public bool IsDefault { get; set; }
        }

        public record CommandWithUserId(Command Command, string UserId) : IRequest<Result<int>>;

        private class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Name)
                    .NotEmpty()
                    .WithMessage("Name for address is required")
                    .MaximumLength(LengthConstants.AddressName)
                    .WithMessage($"Address name length can't exceed {LengthConstants.AddressName}");

                RuleFor(x => x.Street)
                    .NotEmpty()
                    .WithMessage("Street is required")
                    .MaximumLength(LengthConstants.Street)
                    .WithMessage($"Street length can't exceed {LengthConstants.Street}");

                RuleFor(x => x.Governorate)
                    .NotEmpty()
                    .WithMessage("Governorate is required")
                    .MaximumLength(LengthConstants.Governorate)
                    .WithMessage($"Governorate length can't exceed {LengthConstants.Governorate}");

                RuleFor(x => x.BuildingNumber)
                    .NotEmpty()
                    .WithMessage("Building number is required")
                    .MaximumLength(LengthConstants.BuildingNumber)
                    .WithMessage($"Building Number length can't exceed {LengthConstants.BuildingNumber}");

                RuleFor(x => x.AptNumber)
                    .NotEmpty()
                    .WithMessage("Apartment number is required")
                    .MaximumLength(LengthConstants.AptNumber)
                    .WithMessage($"Apartment number length can't exceed {LengthConstants.AptNumber}");

                RuleFor(x => x.FloorNumber)
                    .NotEmpty()
                    .WithMessage("Floor number is required")
                    .MaximumLength(LengthConstants.FloorNumber)
                    .WithMessage($"Floor number length can't exceed {LengthConstants.FloorNumber}");
            }
        }

        internal sealed class Handler(AppDbContext dbContext, ILogger<AddUserAddressEndpoint> logger)
            : IRequestHandler<CommandWithUserId, Result<int>>
        {
            public async Task<Result<int>> Handle(CommandWithUserId request, CancellationToken cancellationToken)
            {
                var cmd = request.Command;

                using var scope = logger.BeginScope("UserId: {UserId}", request.UserId);
                logger.LogInformation("Attempting to add new address");

                var validator = new Validator();
                var validationResult = await validator.ValidateAsync(cmd, cancellationToken);
                if (!validationResult.IsValid)
                {
                    logger.LogInformation("Invalid request");
                    return Result.BadRequest<int>(validationResult.ToString());
                }
                var existingAddressWithSameName = await dbContext.UserAddresses
                    .FirstOrDefaultAsync(x => x.UserId == request.UserId && x.Name == cmd.Name, cancellationToken);

                if (existingAddressWithSameName is not null)
                {
                    logger.LogInformation("Address name '{AddressName}' already exists for user", cmd.Name);
                    return Result.BadRequest<int>("An address with this name already exists.");
                }

                if (cmd.IsDefault)
                {
                    var existingAddresses = await dbContext.UserAddresses
                        .Where(x => x.UserId == request.UserId)
                        .ToListAsync(cancellationToken);

                    foreach (var address in existingAddresses)
                    {
                        address.IsDefault = false;
                    }
                }

                var userAddress = new UserAddress
                {
                    Name = cmd.Name!,
                    Street = cmd.Street!,
                    Governorate = cmd.Governorate!,
                    BuildingNumber = cmd.BuildingNumber!,
                    AptNumber = cmd.AptNumber!,
                    FloorNumber = cmd.FloorNumber!,
                    IsDefault = cmd.IsDefault,
                    UserId = request.UserId
                };

                dbContext.UserAddresses.Add(userAddress);
                await dbContext.SaveChangesAsync(cancellationToken);

                logger.LogInformation("Successfully added address {AddressId}", userAddress.Id);

                return Result.Created(userAddress.Id);
            }
        }
    }

    public class AddUserAddressEndpoint : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("api/addresses", async (
                    Contracts.AddUserAddress request,
                    ISender sender,
                    ILogger<AddUserAddressEndpoint> logger,
                    ClaimsPrincipal user) =>
                {
                    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

                    if (string.IsNullOrEmpty(userId))
                    {
                        logger.LogWarning("User ID is missing in the token");
                        return Results.Unauthorized();
                    }

                    logger.LogInformation("Received request to add new address for user {UserId}", userId);

                    var command = request.Adapt<AddUserAddress.Command>();

                    var result = await sender.Send(new AddUserAddress.CommandWithUserId(command, userId));

                    return result.Resolve();
                })
                .RequireAuthorization()
                .WithName("AddUserAddress")
                .WithOpenApi(operation => new OpenApiOperation(operation)
                {
                    Summary = "Add a new address",
                    Description = "Adds a new address for the authenticated user"
                });
        }
    }
}
