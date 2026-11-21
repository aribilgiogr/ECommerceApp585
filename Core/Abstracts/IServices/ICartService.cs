using Core.Concretes.DTOs.Cart;

namespace Core.Abstracts.IServices;

public interface ICartService
{
    Task<CartResponseDto> GetUserCartAsync(Guid userId);
    Task<CartResponseDto> UpsertItemAsync(Guid userId, CartItemUpsertDto dto);
    Task<CartResponseDto> RemoveItemAsync(Guid userId, Guid productId);
    Task ClearCartAsync(Guid userId);
}