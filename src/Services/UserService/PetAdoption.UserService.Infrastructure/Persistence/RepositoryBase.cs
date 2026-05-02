namespace PetAdoption.UserService.Infrastructure.Persistence;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

public abstract class RepositoryBase
{
    protected readonly UserServiceDbContext _db;

    protected RepositoryBase(UserServiceDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Attaches or marks the entity for update/insert depending on whether it already exists
    /// in the database. Only acts when the entity is detached (not already tracked).
    /// </summary>
    protected async Task UpsertAsync<TEntity>(
        DbSet<TEntity> dbSet,
        TEntity entity,
        Expression<Func<TEntity, bool>> existsPredicate)
        where TEntity : class
    {
        if (_db.Entry(entity).State == EntityState.Detached)
        {
            bool exists = await dbSet.AnyAsync(existsPredicate);
            if (exists) dbSet.Update(entity);
            else dbSet.Add(entity);
        }
    }
}
