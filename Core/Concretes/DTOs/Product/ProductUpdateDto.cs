namespace Core.Concretes.DTOs.Product;

public record ProductUpdateDto(
    Guid Id,
    string Name,
    string? Description,
    string SKU,
    string? CoverImageUrl,
    decimal Price,
    decimal DiscountRate,
    decimal TaxRate,
    int StockQuantity,
    Guid CategoryId,
    Guid BrandId,
    IEnumerable<ProductAttributeDto> Attributes,
    IEnumerable<ProductImageDto> Images,
    bool Active
);