using System.Security.Claims;
using Carter;
using FishShop.API.Contracts;
using FishShop.API.Database;
using FishShop.API.Entities;
using FishShop.API.Entities.Enums;
using FishShop.API.Shared;
using FluentValidation;
using MediatR;
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
        public string? AddressId { get; init; }
        public string? PaymentInfo { get; init; }
        public decimal Discount { get; init; }
        public int DeliveryFees { get; init; }
        public string PromoCode { get; init; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.AddressId)
                .NotEmpty()
                .When(x => x.DeliveryType != DeliveryType.PickUp)
                .WithMessage("Address ID is required when delivery type is not office pickup");     

            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage("Order must contain at least one item");

            RuleForEach(x => x.Items)
                .ChildRules(item =>
                {
                    item.RuleFor(x => x.ProductId)
                        .GreaterThan(0)
                        .WithMessage("Invalid product ID");
                    item.RuleFor(x => x.Quantity)
                        .GreaterThan(-1)
                        .WithMessage("Quantity must be greater than 0");
                });
            
            RuleFor(x => x.PaymentInfo)
                .NotEmpty()
                .When(x => x.DeliveryType != DeliveryType.PickUp)
                .WithMessage("Payment information is required for deliveries");
        }
    }

    internal sealed class Handler(AppDbContext dbContext, ILogger<SubmitOrderEndpoint> logger, IHttpContextAccessor httpContextAccessor) : IRequestHandler<Command, Result<int>>
    {
        public async Task<Result<int>> Handle(Command request, CancellationToken cancellationToken)
        {
            var userId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role); // ← GET the role claim

            if (string.IsNullOrEmpty(userId))
            {
                logger.LogWarning("User ID not found in claims");
                return Result.BadRequest<int>("Unauthorized access: user ID not found");
            }

            logger.LogInformation("User {UserId} with role {Role} attempting to submit order", userId, role);

            if (role == "ManagerRole" || role == "AdminRole")
            {
                logger.LogWarning("User {UserId} with role {Role} is not allowed to submit orders", userId, role);
                return Result.BadRequest<int>("Manager and Admins are not allowed to submit orders");
            }
            logger.LogInformation("Attempting to submit order for user {UserId} with {ItemCount} items",
                request.UserId, request.Items!.Count);

            var validator = new Validator();
            var validationResult = await validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                logger.LogInformation("Invalid request, '{UserId}'", request.UserId);
                return Result.BadRequest<int>(validationResult.ToString());
            }

            var productIds = request.Items.Select(i => i.ProductId).ToList();

            var products = await dbContext.Products
                .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
                .Select(p => new { p.Id, p.Price, p.Quantity })
                .ToDictionaryAsync(p => p.Id, p => new { p.Price, p.Quantity }, cancellationToken);

            // Validate all products exist and have sufficient stock
            foreach (var item in request.Items)
            {
                if (!products.ContainsKey(item.ProductId))
                {
                    logger.LogWarning("Product {ProductId} not found or is deleted", item.ProductId);
                    return Result.NotFound<int>($"Product {item.ProductId} not found");
                }
            }
            if (!string.IsNullOrWhiteSpace(request.PromoCode))
            {
                var promo = await dbContext.PromoCodes
                    .FirstOrDefaultAsync(p => p.Code == request.PromoCode && p.IsActive, cancellationToken);

                if (promo is null)
                {
                    logger.LogWarning("Promo code {PromoCode} not found or inactive", request.PromoCode);
                    return Result.NotFound<int>($"Promo code {request.PromoCode} not found or inactive");
                }

                promo.TimesUsed++;
                dbContext.PromoCodes.Update(promo);
            }
            
            decimal totalPrice = request.Items.Sum(i => i.Quantity * products[i.ProductId].Price);
            
            var order = new Order
            {
                UserId = request.UserId,
                AddressId = request.AddressId != null? request.AddressId: "",
                CreatedAt = DateTime.UtcNow,
                DeliveryType = request.DeliveryType,
                DeliveryFees = request.DeliveryFees,
                Discount = request.Discount,
                TotalPrice = totalPrice + request.DeliveryFees - request.Discount,
                Status = OrderStatus.Pending,
                PaymentInfo = request.PaymentInfo,
                PromoCode = request.PromoCode,
                Products = request.Items.Select(i => new OrderProduct
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = products[i.ProductId].Price
                }).ToList()
            };

            dbContext.Orders.Add(order);

            await dbContext.SaveChangesAsync(cancellationToken);


            logger.LogInformation("Successfully submitted order {OrderId} for user {UserId}",
                order.Id, request.UserId);

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

                logger.LogInformation("User {UserId} attempting to submit order", userId);

                var command = new SubmitOrder.Command
                {
                    UserId = userId,
                    AddressId = request.AddressId,
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