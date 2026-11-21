using Core.Concretes.DTOs.Category;
using Utilities._585.Models;

namespace Core.Abstracts.IServices;

public interface ICategoryService
{
    Task<IEnumerable<CategoryResponseDto>> GetAllAsync();
    Task<IEnumerable<CategoryResponseDto>> GetCategoryTreeAsync();
    Task<CategoryResponseDto?> GetByIdAsync(Guid id);
    Task<Reply<CategoryResponseDto>> CreateAsync(CategoryCreateDto dto);
    Task<Reply<CategoryResponseDto>> UpdateAsync(CategoryUpdateDto dto);
    Task<Reply> DeleteAsync(Guid id);
}