using Carter;
using FishShop.API.Contracts;
using FishShop.API.Database;
using FishShop.API.Entities;
using FishShop.API.Shared;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Security.Claims;

namespace FishShop.API.Features;

public static class GetUsers
{
    public record Query : IRequest<Result<PagedResult<UserDto>>>
    {
        public string? Role { get; set; }
        public string? Email { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public bool SortByOrdersCountDesc { get; set; } = false;
    }

    internal sealed class Handler(AppDbContext db, UserManager<User> userManager, ILogger<GetUsersEndpoint> logger)
        : IRequestHandler<Query, Result<PagedResult<UserDto>>>
    {
        public async Task<Result<PagedResult<UserDto>>> Handle(Query request, CancellationToken ct)
        {
            // Base query: include orders and their related order products and product sizes
            var baseQuery = db.Users
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderProducts)
                        .ThenInclude(op => op.ProductSize)
                            .ThenInclude(ps => ps.Product)
                .AsQueryable();

            // Filter by email
            if (!string.IsNullOrWhiteSpace(request.Email))
                baseQuery = baseQuery.Where(u => u.Email!.Contains(request.Email));

            var users = await baseQuery.ToListAsync(ct);

            // Filter by role
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                users = users.Where(u =>
                {
                    var claims = userManager.GetClaimsAsync(u).Result;
                    var role = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                    return role != null && role.Equals(request.Role, StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }

            // Sort
            if (request.SortByOrdersCountDesc)
            {
                logger.LogInformation("Sorting users by orders count descending");
                users = users.OrderByDescending(u => u.Orders.Count).ToList();
            }

            var totalItems = users.Count;
            var pagedUsers = users
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var result = new List<UserDto>();

            foreach (var user in pagedUsers)
            {
                var claims = await userManager.GetClaimsAsync(user);
                var claimsDict = claims.ToDictionary(c => c.Type, c => c.Value);

                result.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    DisplayName = claimsDict.GetValueOrDefault("DisplayName") ?? "",
                    Gender = claimsDict.GetValueOrDefault(ClaimTypes.Gender) ?? "",
                    PhoneNumber = user.PhoneNumber ?? "",
                    JoinDate = claimsDict.GetValueOrDefault("JoinDate") ?? "",
                    Age = claimsDict.GetValueOrDefault("Age") ?? "",
                    Role = claimsDict.GetValueOrDefault(ClaimTypes.Role) ?? "",
                    OrdersCount = user.Orders.Count,
                    Orders = user.Orders.Select(o => new OrderDetails
                    {
                        Id = o.Id,
                        Address = o.Address,
                        Status = o.Status,
                        CreatedAt = o.CreatedAt,
                        TotalAmount = o.TotalPrice,
                        PaymentInfo = o.PaymentInfo ?? "",
                        DeliveryDate = o.DeliveryDate,
                        Items = o.OrderProducts?.Select(op => new OrderItem
                        {
                            ProductId = op.ProductSize.ProductId,
                            SizeName = op.ProductSize.SizeName,
                            Quantity = op.Quantity,
                            UnitPrice = op.UnitPrice
                        }).ToList() ?? new List<OrderItem>()
                    }).ToList()
                });
            }

            return Result.Success(new PagedResult<UserDto>
            {
                Items = result,
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = totalItems
            });
        }
    }
}

public class GetUsersEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/users", async (
                [AsParameters] GetUsers.Query query,
                ISender sender) =>
            {
                var result = await sender.Send(query);
                return result.Resolve();
            })
            .RequireAuthorization(PolicyConstants.ManagerPolicy)
            .WithName("GetUsers")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Get paginated users",
                Description = "Retrieves paginated list of users with optional email, role filtering, and sorting by number of orders"
            });
    }
}
