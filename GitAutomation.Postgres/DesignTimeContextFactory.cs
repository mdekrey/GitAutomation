using GitAutomation.EFCore.BranchingModel;
using GitAutomation.EFCore.SecurityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Postgres
{
#if DEBUG

    abstract class GenericDesignTimeDbContextFactory
    {
        internal const string debugConnectionString = @"Host=localhost;Port=35432;Username=postgres;";
        internal static readonly IOptions<PostgresOptions> options = Options.Create(new PostgresOptions { ConnectionString = debugConnectionString });
        internal DbContextOptions CreateOptions(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseNpgsql(debugConnectionString, b => b.MigrationsAssembly(this.GetType().Assembly.FullName));
            return optionsBuilder.Options;
        }
    }

    class NpgsqlBranchingConfiguration : GenericDesignTimeDbContextFactory, IDesignTimeDbContextFactory<BranchingContext>
    {
        public BranchingContext CreateDbContext(string[] args)
        {
            return new BranchingContext(new PostgresBranchingContextCustomization(options));
        }
    }

    class NpgsqlSecurityConfiguration : GenericDesignTimeDbContextFactory, IDesignTimeDbContextFactory<SecurityContext>
    {
        public SecurityContext CreateDbContext(string[] args)
        {
            return new SecurityContext(new PostgresSecurityContextCustomization(options));
        }
    }
#endif
}
