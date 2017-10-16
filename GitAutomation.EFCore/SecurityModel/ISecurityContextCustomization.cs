using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.EFCore.SecurityModel
{
    public interface ISecurityContextCustomization
    {
        DbContextOptions Options { get; }

        void OnModelCreating(ModelBuilder modelBuilder);
    }
}
