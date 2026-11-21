using Core.Abstracts.Bases;

namespace Core.Concretes.Entities;

public class ProductReview : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public int Rating { get; set; } // e.g., 1 to 5
    public string? Comment { get; set; }
}