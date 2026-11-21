using Core.Abstracts.Bases;

namespace Core.Concretes.Entities;

public class ProductImage : BaseEntity
{
    public string ImageUrl { get; set; } = null!;
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; } = 0;

    // Foreign key for Product
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
}