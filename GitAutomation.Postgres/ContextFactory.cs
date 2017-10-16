using GitAutomation.EFCore.BranchingModel;
using GitAutomation.EFCore.SecurityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Postgres
{
    class ContextFactory
    {
        private readonly DbContextOptions dbContextOptions;

        public ContextFactory(IOptions<PostgresOptions> options)
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseNpgsql(options.Value.ConnectionString, b => b.MigrationsAssembly(this.GetType().Assembly.FullName));
            dbContextOptions = optionsBuilder.Options;

            GetBranchingContext().Database.Migrate();
        }
        
        public BranchingContext GetBranchingContext()
        {
            return new BranchingContext(dbContextOptions);
        }
    }
}
