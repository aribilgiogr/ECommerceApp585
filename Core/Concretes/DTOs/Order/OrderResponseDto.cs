using Core.Concretes.Enums;

namespace Core.Concretes.DTOs.Order;

public record OrderResponseDto(
    Guid Id,
    string OrderNumber,
    decimal SubTotal,
    decimal TotalTax,
    decimal TotalAmount,
    OrderStatus Status,
    string ShippingAddress,
    DateTime CreatedAt,
    IEnumerable<OrderItemResponseDto> Items
);