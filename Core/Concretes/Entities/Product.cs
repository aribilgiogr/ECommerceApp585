using System.ComponentModel.DataAnnotations.Schema;
using Core.Abstracts.Bases;

namespace Core.Concretes.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string SKU { get; set; } = null!;
    public string? CoverImageUrl { get; set; }

    public decimal Price { get; set; }
    public decimal DiscountRate { get; set; } = 0;
    public decimal TaxRate { get; set; } = 20;

    [NotMapped]
    public decimal DiscountedPrice => Math.Round(Price * (1 - DiscountRate / 100m), 2);

    [NotMapped]
    public decimal FinalPriceWithTax => Math.Round(DiscountedPrice * (1 + TaxRate / 100m), 2);

    public int StockQuantity { get; set; }

    // Foreign key for Category
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    // Foreign key for Brand
    public Guid BrandId { get; set; }
    public Brand? Brand { get; set; }

    // Navigation properties for related entities
    public ICollection<ProductImage> Images { get; set; } = [];
    public ICollection<ProductAttribute> Attributes { get; set; } = [];
    public ICollection<ProductReview> Reviews { get; set; } = [];
    public ICollection<CartItem> CartItems { get; set; } = [];
    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
