using Core.Concretes.Enums;

namespace Core.Concretes.DTOs.Auth;

public record UserProfileDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    UserRole Role,
    DateTime CreatedAt
);