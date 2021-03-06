﻿using GitAutomation.EFCore.BranchingModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.Postgres
{
    class PostgresBranchingContextCustomization : IBranchingContextCustomization
    {
        private readonly IOptions<PostgresOptions> options;

        public PostgresBranchingContextCustomization(IOptions<PostgresOptions> options)
        {
            this.options = options;
        }

        public DbContextOptions Options
        {
            get
            {
                var optionsBuilder = new DbContextOptionsBuilder();
                optionsBuilder.UseNpgsql(options.Value.ConnectionString, b => b.MigrationsAssembly(this.GetType().Assembly.FullName));
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
