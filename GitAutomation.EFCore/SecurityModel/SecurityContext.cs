using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GitAutomation.EFCore.SecurityModel
{
    public partial class SecurityContext : DbContext
    {
        private readonly ISecurityContextCustomization customization;

        public virtual DbSet<User> User { get; set; }
        public virtual DbSet<UserRole> UserRole { get; set; }

        public SecurityContext(ISecurityContextCustomization customization) : base(customization.Options)
        {
            this.customization = customization;
        }
        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.ClaimName);

                entity.Property(e => e.ClaimName)
                    .HasMaxLength(256)
                    .ValueGeneratedNever();
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => new { e.ClaimName, e.Role });

                entity.Property(e => e.ClaimName).HasMaxLength(256);

                entity.Property(e => e.Role)
                    .HasMaxLength(50)
                    .IsUnicode(false);

                entity.HasOne(d => d.ClaimNameNavigation)
                    .WithMany(p => p.UserRole)
                    .HasForeignKey(d => d.ClaimName)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("FK_UserRole_ToUser");
            });

            customization.OnModelCreating(modelBuilder);
        }
    }
}
