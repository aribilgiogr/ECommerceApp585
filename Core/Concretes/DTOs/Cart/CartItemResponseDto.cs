namespace Core.Concretes.DTOs.Cart;

public record CartItemResponseDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? CoverImageUrl,
    decimal UnitPrice,
    decimal DiscountRate,
    decimal DiscountedUnitPrice,
    decimal TaxRate,
    decimal FinalUnitPriceWithTax,
    int Quantity,
    decimal LineTotalWithTax
);