using Core.Concretes.Entities;
using Microsoft.EntityFrameworkCore;

namespace Data.Contexts;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.Deleted);
        modelBuilder.Entity<Category>().HasQueryFilter(e => !e.Deleted);
        modelBuilder.Entity<Brand>().HasQueryFilter(e => !e.Deleted);
        modelBuilder.Entity<Product>().HasQueryFilter(e => !e.Deleted);
        modelBuilder.Entity<ProductAttribute>().HasQueryFilter(e => !e.Deleted);
        modelBuilder.Entity<ProductImage>().HasQueryFilter(e => !e.Deleted);
        modelBuilder.Entity<ProductReview>().HasQueryFilter(e => !e.Deleted);
        modelBuilder.Entity<Cart>().HasQueryFilter(e => !e.Deleted);
        modelBuilder.Entity<CartItem>().HasQueryFilter(e => !e.Deleted);
        modelBuilder.Entity<Order>().HasQueryFilter(e => !e.Deleted);
        modelBuilder.Entity<OrderItem>().HasQueryFilter(e => !e.Deleted);
    }
}
