using FishShop.API.Entities.Enums;

namespace FishShop.API.Features;

using Carter;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Database;
using Shared;
public record MonthlySalesDto(string Month, decimal TotalSales);

public record Query(int Year) : IRequest<Result<List<MonthlySalesDto>>>;

public static class GetMonthlySales
{
    public class Handler(AppDbContext dbContext, ILogger<GetOrdersEndpoint> logger) : IRequestHandler<Query, Result<List<MonthlySalesDto>>>
    {
        public async Task<Result<List<MonthlySalesDto>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var year = request.Year;

            // Fetch delivered orders grouped by month
            var monthlySalesRaw = await dbContext.Orders
                .Where(o => o.Status == OrderStatus.Delivered && o.DeliveryDate.Year == year)
                .GroupBy(o => o.DeliveryDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    TotalSales = g.Sum(o => o.TotalPrice)
                })
                .ToListAsync(cancellationToken);

            // Create a dictionary month -> total sales for quick lookup
            var salesDict = monthlySalesRaw.ToDictionary(ms => ms.Month, ms => ms.TotalSales);

            // Build a list for all 12 months, filling missing months with 0
            var completeMonthlySales = Enumerable.Range(1, 12)
                .Select(m => new MonthlySalesDto(
                    new DateTime(year, m, 1).ToString("MMM"),
                    salesDict.ContainsKey(m) ? salesDict[m] : 0m
                ))
                .ToList();

            return Result<List<MonthlySalesDto>>.Success(completeMonthlySales);
        }
    }
}
public class GetMonthlySalesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/sales/monthly-sales", async (int year, ISender sender) =>
            {
                var result = await sender.Send(new Query(year));
                if (result.IsSuccess)
                    return Results.Ok(result.Value);
                return Results.BadRequest(new { message = result.Error ?? "Something went wrong." });
            })
            .RequireAuthorization(PolicyConstants.ManagerPolicy);
    }
}