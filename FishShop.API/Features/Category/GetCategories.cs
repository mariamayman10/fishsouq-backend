using Carter;
using FishShop.API.Contracts;
using FishShop.API.Database;
using FishShop.API.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FishShop.API.Features;

public static class GetCategories
{
    public record Query : IRequest<Result<List<GetCategory>>>
    {
    }

    internal sealed class Handler(AppDbContext dbContext) : IRequestHandler<Query, Result<List<GetCategory>>>
    {
        public async Task<Result<List<GetCategory>>> Handle(Query request, CancellationToken cancellationToken)
        {
            var categories = await dbContext.Categories
                .Select(c => new GetCategory
                {
                    Id = c.Id,
                    Name = c.Name!
                })
                .ToListAsync(cancellationToken);

            return Result.Success(categories);
        }
    }
}

public class GetCategoriesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/categories", async (ISender sender) =>
        {
            var result = await sender.Send(new GetCategories.Query());
            return result.Resolve();
        });
    }
}