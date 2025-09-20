using FishShop.API.Entities;
using Microsoft.OpenApi.Models;

namespace FishShop.API.Features;

using Carter;
using FishShop.API.Contracts;
using FishShop.API.Database;
using FishShop.API.Shared;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

    internal sealed class Handler(AppDbContext db, UserManager<User> userManager, ILogger<GetOrdersEndpoint> logger)
        : IRequestHandler<Query, Result<PagedResult<UserDto>>>
    {
        public async Task<Result<PagedResult<UserDto>>> Handle(Query request, CancellationToken ct)
        {
            var baseQuery = db.Users
                .Include(u => u.Orders)
                    .ThenInclude(o => o.Products)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                baseQuery = baseQuery.Where(u => u.Email!.Contains(request.Email));
            }

            var users = await baseQuery.ToListAsync(ct);

            // Filter by Role (manual filtering because roles are in claims)
            if (!string.IsNullOrWhiteSpace(request.Role))
            {
                users = users.Where(u =>
                {
                    var claims = userManager.GetClaimsAsync(u).Result;
                    var role = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                    return role != null && role.Equals(request.Role, StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }

            // Sort by number of orders
            if (request.SortByOrdersCountDesc)
            {
                logger.LogInformation("Sorting by orders desc");   
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
                    PhoneNumber = user.PhoneNumber!,
                    JoinDate = claimsDict.GetValueOrDefault("JoinDate") ?? "",
                    Age = claimsDict.GetValueOrDefault("Age") ?? "",
                    Role = claimsDict.GetValueOrDefault(ClaimTypes.Role) ?? "",
                    OrdersCount = user.Orders.Count,
                    Orders = user.Orders.Select(o => new OrderDetails
                    {
                        Id = o.Id,
                        AddressId = o.AddressId,
                        Status = o.Status,
                        CreatedAt = o.CreatedAt,
                        TotalAmount = o.TotalPrice,
                        Items = o.Products?.Select(p => new OrderItem
                        {
                            ProductId = p.ProductId,
                            Quantity = p.Quantity,
                            UnitPrice = p.UnitPrice
                        }).ToList() ?? new List<OrderItem>(),
                        PaymentInfo = o.PaymentInfo ?? "",
                        DeliveryDate = o.DeliveryDate
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
