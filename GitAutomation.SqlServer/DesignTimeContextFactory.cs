using GitAutomation.EFCore.BranchingModel;
using GitAutomation.EFCore.SecurityModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.SqlServer
{
#if DEBUG

    abstract class GenericDesignTimeDbContextFactory
    {
        internal const string debugConnectionString = @"Server=localhost,31433;Database=gitautomation;User Id=sa;Password=weakPASSw0rd;";
        internal static readonly IOptions<SqlServerOptions> options = Options.Create(new SqlServerOptions { ConnectionString = debugConnectionString });
        internal DbContextOptions CreateOptions(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlServer(debugConnectionString, b => b.MigrationsAssembly(this.GetType().Assembly.FullName));
            return optionsBuilder.Options;
        }
    }

    class NpgsqlBranchingConfiguration : GenericDesignTimeDbContextFactory, IDesignTimeDbContextFactory<BranchingContext>
    {
        public BranchingContext CreateDbContext(string[] args)
        {
            return new BranchingContext(new SqlBranchingContextCustomization(options));
        }
    }

    class NpgsqlSecurityConfiguration : GenericDesignTimeDbContextFactory, IDesignTimeDbContextFactory<SecurityContext>
    {
        public SecurityContext CreateDbContext(string[] args)
        {
            return new SecurityContext(new SqlSecurityContextCustomization(options));
        }
    }
#endif
}
