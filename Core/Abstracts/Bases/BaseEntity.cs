using System.ComponentModel.DataAnnotations;

namespace Core.Abstracts.Bases;

public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool Active { get; set; } = true;
    public bool Deleted { get; set; } = false;        
}
