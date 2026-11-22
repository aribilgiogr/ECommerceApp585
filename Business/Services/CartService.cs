using AutoMapper;
using Core.Abstracts.IServices;
using Core.Concretes.DTOs.Cart;
using Core.Concretes.Entities;
using Microsoft.EntityFrameworkCore;
using Utilities._585.Data.UnitOfWorks;

namespace Business.Services;

public class CartService(IUnitOfWork unitOfWork, IMapper mapper) : ICartService
{
    private async Task<Cart> GetOrCreateTrackedCartAsync(Guid userId)
    {
        var userExists = await unitOfWork
            .GetRepository<User>()
            .AnyAsync(u => u.Id == userId && u.Active);
        if (!userExists)
        {
            throw new KeyNotFoundException("User not found or inactive!");
        }

        var cartRepo = unitOfWork.GetRepository<Cart>();
        var cart = await cartRepo
            .GetAll(true)
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Active);

        if (cart != null)
        {
            return cart;
        }

        cart = new Cart { UserId = userId };
        await cartRepo.CreateAsync(cart);
        var reply = await unitOfWork.CommitAsync();
        if (!reply.IsSuccess)
        {
            throw new InvalidOperationException(string.Join("; ", reply.Errors ?? []));
        }

        return cart;
    }

    private CartResponseDto MapToCartResponseDto(Cart cart)
    {
        var validItems = cart
            .Items.Where(i => !i.Deleted && i.Product is { Deleted: false, Active: true })
            .OrderBy(i => i.CreatedAt)
            .ToList();

        var itemDtos = mapper.Map<List<CartItemResponseDto>>(validItems);
        var subTotal = validItems.Sum(i => i.Product.DiscountedPrice * i.Quantity);
        var totalTax = validItems.Sum(i =>
            (i.Product.FinalPriceWithTax - i.Product.DiscountedPrice) * i.Quantity
        );
        var grandTotal = subTotal + totalTax;

        return new CartResponseDto(cart.Id, cart.UserId, itemDtos, subTotal, totalTax, grandTotal);
    }

    public async Task<CartResponseDto> GetUserCartAsync(Guid userId)
    {
        var userExists = await unitOfWork
            .GetRepository<User>()
            .AnyAsync(u => u.Id == userId && u.Active);
        if (!userExists)
        {
            throw new KeyNotFoundException("User not found or inactive!");
        }

        var cart = await unitOfWork
            .GetRepository<Cart>()
            .GetAll()
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = await GetOrCreateTrackedCartAsync(userId);
        }

        return MapToCartResponseDto(cart);
    }

    public async Task<CartResponseDto> UpsertItemAsync(Guid userId, CartItemUpsertDto dto)
    {
        var product = await unitOfWork
            .GetRepository<Product>()
            .GetAll()
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId);

        if (product == null)
        {
            throw new KeyNotFoundException("Product not found!");
        }

        var cart = await GetOrCreateTrackedCartAsync(userId);
        var cartRepo = unitOfWork.GetRepository<Cart>();
        var cartItemRepo = unitOfWork.GetRepository<CartItem>();
        var existingItem = cart.Items.FirstOrDefault(i =>
            !i.Deleted && i.ProductId == dto.ProductId
        );

        if (existingItem == null)
        {
            if (dto.Quantity <= 0)
            {
                return MapToCartResponseDto(cart);
            }

            if (!product.Active)
            {
                throw new InvalidOperationException("Product is not active!");
            }

            if (product.StockQuantity < dto.Quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for '{product.Name}'. Available: {product.StockQuantity}"
                );
            }

            cart.UpdatedAt = DateTime.UtcNow;
            cartRepo.Update(cart);

            var newItem = new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                Quantity = dto.Quantity,
            };
            await cartItemRepo.CreateAsync(newItem);
        }
        else
        {
            var newQuantity = existingItem.Quantity + dto.Quantity;
            if (newQuantity <= 0)
            {
                existingItem.Deleted = true;
                existingItem.Active = false;
                existingItem.UpdatedAt = DateTime.UtcNow;
                cartItemRepo.Update(existingItem);
            }
            else
            {
                if (dto.Quantity > 0)
                {
                    if (!product.Active)
                    {
                        throw new InvalidOperationException("Product is not active!");
                    }

                    if (product.StockQuantity < newQuantity)
                    {
                        throw new InvalidOperationException(
                            $"Insufficient stock for '{product.Name}'. Available: {product.StockQuantity}"
                        );
                    }
                }

                existingItem.Quantity = newQuantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
                cartItemRepo.Update(existingItem);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            cartRepo.Update(cart);
        }

        var reply = await unitOfWork.CommitAsync();
        if (!reply.IsSuccess)
        {
            throw new InvalidOperationException(string.Join("; ", reply.Errors ?? []));
        }

        return await GetUserCartAsync(userId);
    }

    public async Task<CartResponseDto> RemoveItemAsync(Guid userId, Guid productId)
    {
        var cart = await GetOrCreateTrackedCartAsync(userId);
        var item = cart.Items.FirstOrDefault(i => !i.Deleted && i.ProductId == productId);

        if (item == null)
        {
            return MapToCartResponseDto(cart);
        }

        var now = DateTime.UtcNow;
        item.Deleted = true;
        item.Active = false;
        item.UpdatedAt = now;
        unitOfWork.GetRepository<CartItem>().Update(item);

        cart.UpdatedAt = now;
        unitOfWork.GetRepository<Cart>().Update(cart);

        var reply = await unitOfWork.CommitAsync();
        if (!reply.IsSuccess)
        {
            throw new InvalidOperationException(string.Join("; ", reply.Errors ?? []));
        }

        return await GetUserCartAsync(userId);
    }

    public async Task ClearCartAsync(Guid userId)
    {
        var cartRepo = unitOfWork.GetRepository<Cart>();
        var cart = await cartRepo
            .GetAll(true)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            return;
        }

        var activeItems = cart.Items.Where(i => !i.Deleted).ToList();
        if (activeItems.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var cartItemRepo = unitOfWork.GetRepository<CartItem>();
        foreach (var item in activeItems)
        {
            item.Deleted = true;
            item.Active = false;
            item.UpdatedAt = now;
            cartItemRepo.Update(item);
        }

        cart.UpdatedAt = now;
        cartRepo.Update(cart);

        var reply = await unitOfWork.CommitAsync();
        if (!reply.IsSuccess)
        {
            throw new InvalidOperationException(string.Join("; ", reply.Errors ?? []));
        }
    }
}
