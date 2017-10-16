using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore
{

    public static class DbSetExtensions
    {
        public static async Task<T> AddIfNotExists<T>(this DbSet<T> dbSet, T entity, Expression<Func<T, bool>> predicate = null) 
            where T : class, new()
        {
            var exists = await (predicate != null ? dbSet.FirstOrDefaultAsync(predicate) : dbSet.FirstOrDefaultAsync()).ConfigureAwait(false);
            if (exists != null)
            {
                return exists;
            }

            dbSet.Add(entity);
            return entity;
        }
    }
}
