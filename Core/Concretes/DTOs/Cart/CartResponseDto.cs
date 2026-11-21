namespace Core.Concretes.DTOs.Cart;

public record CartResponseDto(
    Guid Id,
    Guid UserId,
    IEnumerable<CartItemResponseDto> Items,
    decimal SubTotal,
    decimal TotalTax,
    decimal GrandTotal
);