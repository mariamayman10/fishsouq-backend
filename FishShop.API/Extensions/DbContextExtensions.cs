using FishShop.API.Shared;
using Microsoft.EntityFrameworkCore;

namespace FishShop.API.Common.Extensions;

public static class DbContextExtensions
{
    public static void SoftDelete<TEntity>(this DbContext context, TEntity entity)
        where TEntity : class, ISoftDelete
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        context.Update(entity);
    }
}