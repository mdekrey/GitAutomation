using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GitAutomation.EFCore.BranchingModel
{
    public partial class BranchingContext : DbContext
    {
        public virtual DbSet<BranchGroup> BranchGroup { get; set; }
        public virtual DbSet<BranchStream> BranchStream { get; set; }

        public BranchingContext(DbContextOptions options) : base(options)
        {
        }
        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BranchGroup>(entity =>
            {
                entity.HasKey(e => e.GroupName);

                entity.Property(e => e.GroupName)
                    .HasMaxLength(255)
                    .ValueGeneratedNever();

                entity.Property(e => e.BranchType)
                    .IsRequired()
                    .HasMaxLength(255)
                    .IsUnicode(false)
                    .HasDefaultValueSql("('Feature')");

                entity.Property(e => e.RecreateFromUpstream).HasDefaultValueSql("((0))");
            });

            modelBuilder.Entity<BranchStream>(entity =>
            {
                entity.HasKey(e => new { e.DownstreamBranch, e.UpstreamBranch });

                entity.HasIndex(e => e.UpstreamBranch);

                entity.Property(e => e.DownstreamBranch).HasMaxLength(255);

                entity.Property(e => e.UpstreamBranch).HasMaxLength(255);

                entity.HasOne(d => d.DownstreamBranchNavigation)
                    .WithMany(p => p.BranchStreamDownstreamBranchNavigation)
                    .HasForeignKey(d => d.DownstreamBranch)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BranchStream_ToDownstreamBranch");

                entity.HasOne(d => d.UpstreamBranchNavigation)
                    .WithMany(p => p.BranchStreamUpstreamBranchNavigation)
                    .HasForeignKey(d => d.UpstreamBranch)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_BranchStream_ToUpstreamBranch");
            });
        }
    }
}
