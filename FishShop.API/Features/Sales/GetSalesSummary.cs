namespace FishShop.API.Features;
using System.Security.Claims;
using Carter;
using FishShop.API.Contracts;
using FishShop.API.Database;
using FishShop.API.Entities.Enums;
using FishShop.API.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
public static class GetTotalSales
{
    public record Query : IRequest<Result<decimal>>;

    internal sealed class Handler(AppDbContext dbContext)
        : IRequestHandler<Query, Result<decimal>>
    {
        public async Task<Result<decimal>> Handle(Query request, CancellationToken cancellationToken)
        {
            var totalSales = await dbContext.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .SumAsync(o => o.TotalPrice, cancellationToken);

            return Result.Success(totalSales);
        }
    }
}
public class GetSalesSummaryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/sales/sales-summary", async (ISender sender) =>
            {
                var result = await sender.Send(new GetTotalSales.Query());
                return result.Resolve();
            })
            .RequireAuthorization(PolicyConstants.ManagerPolicy)
            .WithName("GetTotalSalesSummary")
            .WithOpenApi(operation => new OpenApiOperation(operation)
            {
                Summary = "Get total delivered sales",
                Description = "Returns the total value of delivered orders"
            });
    }
}
