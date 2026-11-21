namespace Core.Concretes.DTOs.Category;

public record CategoryResponseDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    IEnumerable<CategoryResponseDto> SubCategories
);