using AutoMapper;
using Core.Abstracts.IServices;
using Core.Concretes.DTOs.Product;
using Core.Concretes.Entities;
using Microsoft.EntityFrameworkCore;
using Utilities._585.Data.UnitOfWorks;
using Utilities._585.Models;

namespace Business.Services;

public class ProductService(IUnitOfWork unitOfWork, IMapper mapper) : IProductService
{
    private IQueryable<Product> BuildProductQuery(bool enableTracking = false) =>
        unitOfWork
            .GetRepository<Product>()
            .GetAll(enableTracking)
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Attributes.OrderBy(a => a.DisplayOrder))
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder));

    private async Task<string?> ValidateProductReferencesAndValuesAsync(
        string name,
        string sku,
        decimal price,
        decimal discountRate,
        decimal taxRate,
        int stockQuantity,
        Guid categoryId,
        Guid brandId,
        Guid? excludeProductId = null
    )
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Product name is required!";
        }

        if (string.IsNullOrWhiteSpace(sku))
        {
            return "Product SKU is required!";
        }

        if (price < 0 || discountRate < 0 || discountRate > 100 || taxRate < 0 || stockQuantity < 0)
        {
            return "Price, discount rate (0-100), tax rate, and stock quantity must be valid non-negative values!";
        }

        var normalizedSkuLower = sku.Trim().ToLowerInvariant();
        var productRepo = unitOfWork.GetRepository<Product>();
        var skuExists = await productRepo.AnyAsync(p =>
            (!excludeProductId.HasValue || p.Id != excludeProductId.Value)
            && p.SKU.ToLower() == normalizedSkuLower
        );
        if (skuExists)
        {
            return "A product with this SKU already exists!";
        }

        var categoryExists = await unitOfWork
            .GetRepository<Category>()
            .AnyAsync(c => c.Id == categoryId);
        if (!categoryExists)
        {
            return "Category not found!";
        }

        var brandExists = await unitOfWork.GetRepository<Brand>().AnyAsync(b => b.Id == brandId);
        if (!brandExists)
        {
            return "Brand not found!";
        }

        return null;
    }

    public async Task<IEnumerable<ProductResponseDto>> GetAllAsync()
    {
        var products = await BuildProductQuery().OrderBy(p => p.Name).ToListAsync();

        return mapper.Map<IEnumerable<ProductResponseDto>>(products);
    }

    public async Task<IEnumerable<ProductResponseDto>> GetByCategoryIdAsync(Guid categoryId)
    {
        var products = await BuildProductQuery()
            .Where(p => p.CategoryId == categoryId)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return mapper.Map<IEnumerable<ProductResponseDto>>(products);
    }

    public async Task<IEnumerable<ProductResponseDto>> GetByBrandIdAsync(Guid brandId)
    {
        var products = await BuildProductQuery()
            .Where(p => p.BrandId == brandId)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return mapper.Map<IEnumerable<ProductResponseDto>>(products);
    }

    public async Task<ProductResponseDto?> GetByIdAsync(Guid id)
    {
        var product = await BuildProductQuery().FirstOrDefaultAsync(p => p.Id == id);

        return product == null ? null : mapper.Map<ProductResponseDto>(product);
    }

    public async Task<ProductResponseDto?> GetBySKUAsync(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return null;
        }

        var normalizedLower = sku.Trim().ToLowerInvariant();
        var product = await BuildProductQuery()
            .FirstOrDefaultAsync(p => p.SKU.ToLower() == normalizedLower);

        return product == null ? null : mapper.Map<ProductResponseDto>(product);
    }

    public async Task<Reply<ProductResponseDto>> CreateAsync(ProductCreateDto dto)
    {
        var validationError = await ValidateProductReferencesAndValuesAsync(
            dto.Name,
            dto.SKU,
            dto.Price,
            dto.DiscountRate,
            dto.TaxRate,
            dto.StockQuantity,
            dto.CategoryId,
            dto.BrandId
        );
        if (validationError != null)
        {
            return Reply<ProductResponseDto>.Fail(validationError);
        }

        var product = mapper.Map<Product>(dto);
        product.Name = dto.Name.Trim();
        product.Description = dto.Description?.Trim();
        product.SKU = dto.SKU.Trim();
        product.CoverImageUrl = dto.CoverImageUrl?.Trim();

        var repo = unitOfWork.GetRepository<Product>();
        await repo.CreateAsync(product);
        var reply = await unitOfWork.CommitAsync();

        if (!reply.IsSuccess)
        {
            return Reply<ProductResponseDto>.Fail(reply.Errors!);
        }

        var created = await GetByIdAsync(product.Id);
        return Reply<ProductResponseDto>.Success(
            created ?? mapper.Map<ProductResponseDto>(product)
        );
    }

    public async Task<Reply<ProductResponseDto>> UpdateAsync(ProductUpdateDto dto)
    {
        var repo = unitOfWork.GetRepository<Product>();
        var product = await repo.GetAll(true)
            .Include(p => p.Attributes)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == dto.Id);

        if (product == null)
        {
            return Reply<ProductResponseDto>.Fail("Product not found!");
        }

        var validationError = await ValidateProductReferencesAndValuesAsync(
            dto.Name,
            dto.SKU,
            dto.Price,
            dto.DiscountRate,
            dto.TaxRate,
            dto.StockQuantity,
            dto.CategoryId,
            dto.BrandId,
            dto.Id
        );
        if (validationError != null)
        {
            return Reply<ProductResponseDto>.Fail(validationError);
        }

        var now = DateTime.UtcNow;
        mapper.Map(dto, product);
        product.Name = dto.Name.Trim();
        product.Description = dto.Description?.Trim();
        product.SKU = dto.SKU.Trim();
        product.CoverImageUrl = dto.CoverImageUrl?.Trim();
        product.UpdatedAt = now;

        var attrRepo = unitOfWork.GetRepository<ProductAttribute>();
        foreach (var existingAttr in product.Attributes.Where(a => !a.Deleted))
        {
            existingAttr.Deleted = true;
            existingAttr.Active = false;
            existingAttr.UpdatedAt = now;
            attrRepo.Update(existingAttr);
        }

        var imageRepo = unitOfWork.GetRepository<ProductImage>();
        foreach (var existingImage in product.Images.Where(i => !i.Deleted))
        {
            existingImage.Deleted = true;
            existingImage.Active = false;
            existingImage.UpdatedAt = now;
            imageRepo.Update(existingImage);
        }

        repo.Update(product);

        if (dto.Attributes != null)
        {
            var newAttrs = mapper.Map<List<ProductAttribute>>(dto.Attributes);
            foreach (var attr in newAttrs)
            {
                attr.ProductId = product.Id;
            }

            if (newAttrs.Count > 0)
            {
                await attrRepo.CreateManyAsync(newAttrs);
            }
        }

        if (dto.Images != null)
        {
            var newImages = mapper.Map<List<ProductImage>>(dto.Images);
            foreach (var image in newImages)
            {
                image.ProductId = product.Id;
            }

            if (newImages.Count > 0)
            {
                await imageRepo.CreateManyAsync(newImages);
            }
        }

        var reply = await unitOfWork.CommitAsync();

        if (!reply.IsSuccess)
        {
            return Reply<ProductResponseDto>.Fail(reply.Errors!);
        }

        var updated = await GetByIdAsync(product.Id);
        return Reply<ProductResponseDto>.Success(
            updated ?? mapper.Map<ProductResponseDto>(product)
        );
    }

    public async Task<Reply<ProductResponseDto>> UpdateStockAsync(
        Guid productId,
        int quantityChange
    )
    {
        var repo = unitOfWork.GetRepository<Product>();
        var product = await repo.GetAll(true).FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
        {
            return Reply<ProductResponseDto>.Fail("Product not found!");
        }

        var newStock = product.StockQuantity + quantityChange;
        if (newStock < 0)
        {
            return Reply<ProductResponseDto>.Fail(
                $"Insufficient stock! Current stock: {product.StockQuantity}, requested change: {quantityChange}."
            );
        }

        product.StockQuantity = newStock;
        product.UpdatedAt = DateTime.UtcNow;

        repo.Update(product);
        var reply = await unitOfWork.CommitAsync();

        if (!reply.IsSuccess)
        {
            return Reply<ProductResponseDto>.Fail(reply.Errors!);
        }

        var updated = await GetByIdAsync(product.Id);
        return Reply<ProductResponseDto>.Success(
            updated ?? mapper.Map<ProductResponseDto>(product)
        );
    }

    public async Task<Reply> DeleteAsync(Guid id)
    {
        var repo = unitOfWork.GetRepository<Product>();
        var product = await repo.GetAll(true)
            .Include(p => p.Attributes)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return Reply.Fail("Product not found!");
        }

        var now = DateTime.UtcNow;
        product.Deleted = true;
        product.Active = false;
        product.UpdatedAt = now;

        var attrRepo = unitOfWork.GetRepository<ProductAttribute>();
        foreach (var attr in product.Attributes.Where(a => !a.Deleted))
        {
            attr.Deleted = true;
            attr.Active = false;
            attr.UpdatedAt = now;
            attrRepo.Update(attr);
        }

        var imageRepo = unitOfWork.GetRepository<ProductImage>();
        foreach (var image in product.Images.Where(i => !i.Deleted))
        {
            image.Deleted = true;
            image.Active = false;
            image.UpdatedAt = now;
            imageRepo.Update(image);
        }

        var cartItemRepo = unitOfWork.GetRepository<CartItem>();
        var activeCartItems = await cartItemRepo
            .GetAll(true)
            .Where(ci => ci.ProductId == id)
            .ToListAsync();
        foreach (var cartItem in activeCartItems)
        {
            cartItem.Deleted = true;
            cartItem.Active = false;
            cartItem.UpdatedAt = now;
            cartItemRepo.Update(cartItem);
        }

        repo.Update(product);
        var reply = await unitOfWork.CommitAsync();

        return reply.IsSuccess ? Reply.Success() : Reply.Fail(reply.Errors!);
    }
}
