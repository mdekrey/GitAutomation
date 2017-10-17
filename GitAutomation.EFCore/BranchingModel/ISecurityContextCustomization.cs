using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace GitAutomation.EFCore.BranchingModel
{
    public interface IBranchingContextCustomization
    {
        DbContextOptions Options { get; }

        void OnModelCreating(ModelBuilder modelBuilder);
    }
}
