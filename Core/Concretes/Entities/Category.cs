using Core.Abstracts.Bases;

namespace Core.Concretes.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    // Self-referencing relationship for parent-child categories
    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; } = [];

    // Navigation property for related products
    public ICollection<Product> Products { get; set; } = [];
}