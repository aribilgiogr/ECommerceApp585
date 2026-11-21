using Core.Concretes.DTOs.Brand;
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
    Task<Reply<BrandResponseDto>> CreateAsync(ProductCreateDto dto);
    Task<Reply<BrandResponseDto>> UpdateAsync(ProductUpdateDto dto);
    Task<Reply<BrandResponseDto>> UpdateStockAsync(Guid productId, int quantityChange);
    Task<Reply> DeleteAsync(Guid id);
}