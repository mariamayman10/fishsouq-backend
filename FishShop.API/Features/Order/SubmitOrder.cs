using System.Security.Claims;
using Carter;
using FishShop.API.Contracts;
using FishShop.API.Database;
using FishShop.API.Entities;
using FishShop.API.Entities.Enums;
using FishShop.API.Infrastructure.Email;
using FishShop.API.Shared;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace FishShop.API.Features;

public static class SubmitOrder
{
    public record Command : IRequest<Result<int>>
    {
        public string? UserId { get; init; }
        public List<CreateOrderItem>? Items { get; init; }
        public DeliveryType DeliveryType { get; init; }
        public Address? Address { get; init; }
        public string? PaymentInfo { get; init; }
        public decimal Discount { get; init; }
        public int DeliveryFees { get; init; }
        public string? PromoCode { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Address)
                .NotNull()
                .When(x => x.DeliveryType != DeliveryType.PickUp)
                .WithMessage("Address ID is required when delivery type is not office pickup");

            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage("Order must contain at least one item");

            RuleForEach(x => x.Items)
                .ChildRules(item =>
                {
                    item.RuleFor(i => i.ProductSizeId)
                        .GreaterThan(0)
                        .WithMessage("Invalid product size ID");

                    item.RuleFor(i => i.Quantity)
                        .GreaterThan(0)
                        .WithMessage("Quantity must be greater than 0");
                });

            RuleFor(x => x.PaymentInfo)
                .NotEmpty()
                .When(x => x.DeliveryType != DeliveryType.PickUp)
                .WithMessage("Payment information is required for deliveries");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<SubmitOrderEndpoint> logger, IHttpContextAccessor httpContextAccessor, ICustomEmailService emailSender)
        : IRequestHandler<Command, Result<int>>
    {
        public async Task<Result<int>> Handle(Command request, CancellationToken cancellationToken)
        {
            var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role);

            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("User ID not found in claims");
                return Result.BadRequest<int>("Unauthorized access: user ID not found");
            }

            if (role is "ManagerRole" or "AdminRole")
            {
                logger.LogWarning("User {UserId} with role {Role} is not allowed to submit orders", userId, role);
                return Result.BadRequest<int>("Managers and Admins are not allowed to submit orders");
            }

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result.BadRequest<int>(validationResult.ToString());
            }

            // Get all product size IDs from request
            var sizeIds = request.Items!.Select(i => i.ProductSizeId).ToList();

            // Fetch corresponding ProductSizes
            var productSizes = await dbContext.ProductSizes
                .Include(ps => ps.Product)
                .Where(ps => sizeIds.Contains(ps.Id) && !ps.Product.IsDeleted)
                .ToDictionaryAsync(ps => ps.Id, cancellationToken);

            // Validate existence
            foreach (var item in request.Items)
            {
                if (!productSizes.ContainsKey(item.ProductSizeId))
                {
                    logger.LogWarning("ProductSize {Id} not found", item.ProductSizeId);
                    return Result.NotFound<int>($"Product size {item.ProductSizeId} not found");
                }
            }

            // Handle promo
            if (!string.IsNullOrWhiteSpace(request.PromoCode))
            {
                var promo = await dbContext.PromoCodes
                    .FirstOrDefaultAsync(p => p.Code == request.PromoCode && p.IsActive, cancellationToken);

                if (promo is null)
                {
                    logger.LogWarning("Promo code {Code} not found or inactive", request.PromoCode);
                    return Result.NotFound<int>($"Promo code {request.PromoCode} not found or inactive");
                }

                promo.TimesUsed++;
                dbContext.PromoCodes.Update(promo);
            }

            // Calculate total
            decimal totalPrice = request.Items.Sum(i => i.Quantity * productSizes[i.ProductSizeId].Price);

            var order = new Order
            {
                UserId = userId,
                Address = request.Address ?? null,
                CreatedAt = DateTime.UtcNow,
                DeliveryType = request.DeliveryType,
                DeliveryFees = request.DeliveryFees,
                Discount = request.Discount,
                TotalPrice = totalPrice + request.DeliveryFees - request.Discount,
                Status = OrderStatus.Pending,
                PaymentInfo = request.PaymentInfo,
                PromoCode = request.PromoCode ?? "",
                OrderProducts = request.Items.Select(i => new OrderProduct
                {
                    ProductSizeId = i.ProductSizeId,
                    Quantity = i.Quantity,
                    UnitPrice = productSizes[i.ProductSizeId].Price
                }).ToList()
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync(cancellationToken);
            await emailSender.SendOrderNotificationAsync(userId, order.TotalPrice, "mariamayman3131@gmail.com");

            logger.LogInformation("Successfully submitted order {OrderId} for user {UserId}", order.Id, userId);
            return Result.Success(order.Id);
        }
    }
}

public class SubmitOrderEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/orders", async (
                CreateOrder request,
                ISender sender,
                ILogger<SubmitOrderEndpoint> logger,
                ClaimsPrincipal user) =>
            {
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    logger.LogWarning("Unauthorized attempt to submit order");
                    return Results.Unauthorized();
                }

                var command = new SubmitOrder.Command
                {
                    UserId = userId,
                    Address = request.Address,
                    Items = request.Items,
                    DeliveryType = request.DeliveryType,
                    PaymentInfo = request.PaymentInfo,
                    Discount = request.Discount,
                    DeliveryFees = request.DeliveryFees,
                    PromoCode = request.PromoCode
                };

                var result = await sender.Send(command);
                return result.Resolve();
            })
            .RequireAuthorization()
            .WithName("SubmitOrder")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Submit a new order",
                Description = "Creates a new order with the specified items and delivery details"
            });
    }
}
