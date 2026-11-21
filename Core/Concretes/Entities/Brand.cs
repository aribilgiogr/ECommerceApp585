using Core.Abstracts.Bases;

namespace Core.Concretes.Entities;

public class Brand : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }

    // Navigation property for related products
    public ICollection<Product> Products { get; set; } = [];
}