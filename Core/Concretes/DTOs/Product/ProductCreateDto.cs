namespace Core.Concretes.DTOs.Product;

public record ProductCreateDto(
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
    IEnumerable<ProductImageDto> Images
);