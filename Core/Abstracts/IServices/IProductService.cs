using Core.Concretes.DTOs.Product;
using Utilities._585.Models;

namespace Core.Abstracts.IServices;

public interface IProductService
{
    Task<IEnumerable<ProductResponseDto>> GetAllAsync();
    Task<IEnumerable<ProductResponseDto>> GetByCategoryIdAsync(Guid categoryId);
    Task<IEnumerable<ProductResponseDto>> GetByBrandIdAsync(Guid brandId);
    Task<ProductResponseDto?> GetByIdAsync(Guid id);
    Task<ProductResponseDto?> GetBySKUAsync(string sku);
    Task<Reply<ProductResponseDto>> CreateAsync(ProductCreateDto dto);
    Task<Reply<ProductResponseDto>> UpdateAsync(ProductUpdateDto dto);
    Task<Reply<ProductResponseDto>> UpdateStockAsync(Guid productId, int quantityChange);
    Task<Reply> DeleteAsync(Guid id);
}
