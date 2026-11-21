using Core.Abstracts.Bases;

namespace Core.Concretes.Entities;

public class ProductAttribute : BaseEntity
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public int DisplayOrder { get; set; } = 0;

    // Foreign key for Product
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
}