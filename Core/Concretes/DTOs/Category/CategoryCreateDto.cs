namespace Core.Concretes.DTOs.Category;

public record CategoryCreateDto(string Name, string? Description, Guid? ParentCategoryId);