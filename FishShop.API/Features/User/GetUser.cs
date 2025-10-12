using FishShop.API.Entities;

namespace FishShop.API.Features;

using Carter;
using FishShop.API.Contracts;
using FishShop.API.Database;
using FishShop.API.Shared;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


public static class GetUser
{
    public record Query(string Id) : IRequest<Result<UserDto>>;

    internal sealed class Handler(AppDbContext db, UserManager<User> userManager)
        : IRequestHandler<Query, Result<UserDto>>
    {
public async Task<Result<UserDto>> Handle(Query request, CancellationToken ct)
{
    var user = await db.Users
        .Include(u => u.Orders)
        .ThenInclude(o => o.OrderProducts)
        .FirstOrDefaultAsync(u => u.Id == request.Id, ct);

    if (user is null)
        return Result.NotFound<UserDto>("User not found");

    var identityUser = await userManager.FindByIdAsync(user.Id);
    var claims = await userManager.GetClaimsAsync(identityUser);
    var claimsDict = claims.ToDictionary(c => c.Type, c => c.Value);

    var role = claimsDict.GetValueOrDefault(ClaimTypes.Role) ?? "";

    var result = new UserDto
    {
        Id = user.Id,
        Email = user.Email!,
        DisplayName = claimsDict.GetValueOrDefault("DisplayName") ?? "",
        PhoneNumber = user.PhoneNumber!,
        Gender = claimsDict.GetValueOrDefault(ClaimTypes.Gender) ?? "",
        JoinDate = claimsDict.GetValueOrDefault("JoinDate") ?? "",
        Age = claimsDict.GetValueOrDefault("Age") ?? "",
        Role = role
    };

    if (role == "UserRole")
    {
        // Keep orders for normal users
        result.OrdersCount = user.Orders.Count;
        result.Orders = user.Orders.Select(o => new OrderDetails
        {
            Id = o.Id,
            AddressId = o.AddressId,
            Status = o.Status,
            CreatedAt = o.CreatedAt,
            TotalAmount = o.TotalPrice,
            Items = o.OrderProducts?.Select(op => new OrderItem
            {
                ProductId = op.ProductSize.ProductId,
                SizeName = op.ProductSize.SizeName,
                Quantity = op.Quantity,
                UnitPrice = op.UnitPrice
            }).ToList() ?? new List<OrderItem>(),
            PaymentInfo = o.PaymentInfo ?? "",
            DeliveryDate = o.DeliveryDate
        }).ToList();
    }
    else
    {
        // Fetch privileges for Admin/Manager
        var privileges = await db.AdminPrivileges
            .FirstOrDefaultAsync(p => p.AdminId == user.Id, ct);

        if (privileges is not null)
        {
            result.Privileges = new AdminPrivilegesDto
            {
                CanAddProduct = privileges.CanAddProduct,
                CanUpdateProduct = privileges.CanUpdateProduct,
                CanDeleteProduct = privileges.CanDeleteProduct,
                CanAddCategory = privileges.CanAddCategory,
                CanUpdateCategory = privileges.CanUpdateCategory,
                CanDeleteCategory = privileges.CanDeleteCategory,
                CanUpdateOrderStatus = privileges.CanUpdateOrderStatus
            };
        }
    }

    return Result.Success(result);
}
    }
}

public class GetUserEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/users/{id}", async (string id, ISender sender) =>
        {
            var result = await sender.Send(new GetUser.Query(id));
            return result.Resolve();
        }).RequireAuthorization(PolicyConstants.ManagerPolicy);
    }
}
