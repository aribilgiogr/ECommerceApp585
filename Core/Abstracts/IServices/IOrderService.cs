using Core.Concretes.DTOs.Order;
using Core.Concretes.Enums;
using Utilities._585.Models;

namespace Core.Abstracts.IServices;

public interface IOrderService
{
    Task<OrderResponseDto> CreateOrderAsync(Guid userId, OrderCreateDto dto);
    Task<OrderResponseDto?> GetByIdAsync(Guid id);
    Task<OrderResponseDto?> GetByOrderNumberAsync(string orderNumber);
    Task<IEnumerable<OrderResponseDto>> GetUserOrdersAsync(Guid userId);
    Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync();
    Task<Reply<OrderResponseDto>> UpdateOrderStatusAsync(Guid orderId, OrderStatus status);
}