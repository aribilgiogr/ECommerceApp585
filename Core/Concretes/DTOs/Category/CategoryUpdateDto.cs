namespace Core.Concretes.DTOs.Category;

public record CategoryUpdateDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    bool Active
);