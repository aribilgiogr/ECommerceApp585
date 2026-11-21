using Core.Concretes.DTOs.Brand;
using Utilities._585.Models;

namespace Core.Abstracts.IServices;

public interface IBrandService
{
    Task<IEnumerable<BrandResponseDto>> GetAllAsync();
    Task<BrandResponseDto?> GetByIdAsync(Guid id);
    Task<Reply<BrandResponseDto>> CreateAsync(BrandCreateDto dto);
    Task<Reply<BrandResponseDto>> UpdateAsync(BrandUpdateDto dto);
    Task<Reply> DeleteAsync(Guid id);
}