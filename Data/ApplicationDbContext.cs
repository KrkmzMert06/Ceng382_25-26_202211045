using CaterFlow.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaterFlow.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<AppUser> AppUsers => Set<AppUser>();
        public DbSet<CatererProfile> CatererProfiles => Set<CatererProfile>();
        public DbSet<MenuItem> MenuItems => Set<MenuItem>();
        public DbSet<MenuItemCustomizationGroup> MenuItemCustomizationGroups => Set<MenuItemCustomizationGroup>();
        public DbSet<MenuItemCustomizationOption> MenuItemCustomizationOptions => Set<MenuItemCustomizationOption>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUser>()
                .HasIndex(x => x.Email)
                .IsUnique();

            modelBuilder.Entity<CatererProfile>()
                .HasOne(c => c.AppUser)
                .WithMany()
                .HasForeignKey(c => c.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MenuItem>()
                .HasOne(m => m.CatererProfile)
                .WithMany()
                .HasForeignKey(m => m.CatererProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MenuItem>()
                .Property(m => m.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<MenuItemCustomizationGroup>()
                .HasOne(g => g.MenuItem)
                .WithMany(m => m.CustomizationGroups)
                .HasForeignKey(g => g.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MenuItemCustomizationOption>()
                .HasOne(o => o.Group)
                .WithMany(g => g.Options)
                .HasForeignKey(o => o.MenuItemCustomizationGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MenuItemCustomizationOption>()
                .Property(o => o.PriceModifier)
                .HasColumnType("decimal(18,2)");
        }
    }
}
