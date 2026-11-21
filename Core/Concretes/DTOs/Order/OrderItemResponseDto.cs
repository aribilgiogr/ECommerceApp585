namespace Core.Concretes.DTOs.Order;

public record OrderItemResponseDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal OriginalUnitPrice,
    decimal DiscountRate,
    decimal UnitPrice,
    decimal TaxRate,
    decimal TaxAmount
);