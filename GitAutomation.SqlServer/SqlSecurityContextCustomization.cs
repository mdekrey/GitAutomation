﻿using GitAutomation.EFCore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GitAutomation.SqlServer
{
    class SqlSecurityContextCustomization : ISecurityContextCustomization
    {
        private readonly IOptions<SqlServerOptions> options;

        public SqlSecurityContextCustomization(IOptions<SqlServerOptions> options)
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
        }
    }
}
