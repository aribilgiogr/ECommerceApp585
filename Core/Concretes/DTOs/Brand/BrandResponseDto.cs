namespace Core.Concretes.DTOs.Brand;

public record BrandResponseDto(Guid Id, string Name, string? LogoUrl, string? Description);