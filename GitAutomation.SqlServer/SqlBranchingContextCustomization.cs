using GitAutomation.EFCore.BranchingModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.SqlServer
{
    class SqlBranchingContextCustomization : IBranchingContextCustomization
    {
        private readonly IOptions<SqlServerOptions> options;

        public SqlBranchingContextCustomization(IOptions<SqlServerOptions> options)
        {
            this.options = options;
        }

        public DbContextOptions Options
        {
            get
            {
                var optionsBuilder = new DbContextOptionsBuilder();
                optionsBuilder.UseSqlServer(options.Value.ConnectionString, b => b.MigrationsAssembly(this.GetType().Assembly.FullName));
                return optionsBuilder.Options;
            }
        }

        public void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.Relational().TableName = entity.Relational().TableName.ToLower();
                foreach (var property in entity.GetProperties())
                {
                    property.Relational().ColumnName = property.Relational().ColumnName.ToLower();
                }
            }
        }
    }
}
