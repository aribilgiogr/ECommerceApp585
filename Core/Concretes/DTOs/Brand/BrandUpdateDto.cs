namespace Core.Concretes.DTOs.Brand;

public record BrandUpdateDto(Guid Id, string Name, string? LogoUrl, string? Description, bool Active);