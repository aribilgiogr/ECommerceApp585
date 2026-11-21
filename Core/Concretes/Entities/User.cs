using Core.Abstracts.Bases;
using Core.Concretes.Enums;

namespace Core.Concretes.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;

    // JWT Refresh Token Alanları:
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    // Navigation properties for related entities
    public ICollection<Cart> Carts { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
}

