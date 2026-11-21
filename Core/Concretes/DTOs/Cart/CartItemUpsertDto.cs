namespace Core.Concretes.DTOs.Cart;

public record CartItemUpsertDto(Guid ProductId, int Quantity = 1);