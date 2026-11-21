namespace Core.Concretes.DTOs.Product;

public record ProductResponseDto(
    Guid Id,
    string Name,
    string? Description,
    string SKU,
    string? CoverImageUrl,
    decimal Price,
    decimal DiscountRate,
    decimal DiscountedPrice,
    decimal TaxRate,
    decimal FinalPriceWithTax,
    int StockQuantity,
    Guid CategoryId,
    string CategoryName,
    Guid BrandId,
    string BrandName,
    IEnumerable<ProductAttributeDto> Attributes,
    IEnumerable<ProductImageDto> Images
);