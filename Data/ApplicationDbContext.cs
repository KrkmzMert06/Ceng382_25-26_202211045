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
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<OrderItemCustomizationSelection> OrderItemCustomizationSelections => Set<OrderItemCustomizationSelection>();

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

            modelBuilder.Entity<Order>()
                .HasOne(o => o.AppUser)
                .WithMany()
                .HasForeignKey(o => o.AppUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItem>()
                .HasOne(i => i.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(i => i.MenuItem)
                .WithMany()
                .HasForeignKey(i => i.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.UnitPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.CustomizationTotal)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItem>()
                .Property(i => i.LineTotal)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItemCustomizationSelection>()
                .HasOne(s => s.OrderItem)
                .WithMany(i => i.SelectedCustomizations)
                .HasForeignKey(s => s.OrderItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItemCustomizationSelection>()
                .HasOne(s => s.MenuItemCustomizationOption)
                .WithMany()
                .HasForeignKey(s => s.MenuItemCustomizationOptionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OrderItemCustomizationSelection>()
                .Property(s => s.PriceModifier)
                .HasColumnType("decimal(18,2)");
        }
    }
}
