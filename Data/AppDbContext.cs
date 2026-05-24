using Microsoft.EntityFrameworkCore;
using BudgetControl.Api.Models;

namespace BudgetControl.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<UserRole> UserRoles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.Property(u => u.Id).HasColumnName("id");
                entity.Property(u => u.Username).HasColumnName("username");
                entity.Property(u => u.PasswordHash).HasColumnName("password_hash");
                entity.Property(u => u.FullName).HasColumnName("full_name");
                entity.Property(u => u.Email).HasColumnName("email");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");
                entity.Property(r => r.Id).HasColumnName("id");
                entity.Property(r => r.Name).HasColumnName("name");
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("user_roles");
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });
                entity.Property(ur => ur.UserId).HasColumnName("user_id");
                entity.Property(ur => ur.RoleId).HasColumnName("role_id");

                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId);

                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId);
            });
        }
    }
}
