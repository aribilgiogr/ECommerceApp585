using AutoMapper;
using Core.Abstracts.IServices;
using Core.Concretes.DTOs.Order;
using Core.Concretes.Entities;
using Core.Concretes.Enums;
using Microsoft.EntityFrameworkCore;
using Utilities._585.Data.UnitOfWorks;
using Utilities._585.Models;

namespace Business.Services;

public class OrderService(IUnitOfWork unitOfWork, IMapper mapper) : IOrderService
{
    private IQueryable<Order> BuildOrderQuery(bool enableTracking = false) =>
        unitOfWork
            .GetRepository<Order>()
            .GetAll(enableTracking)
            .IgnoreQueryFilters()
            .Where(o => !o.Deleted)
            .Include(o => o.Items.Where(i => !i.Deleted))
            .ThenInclude(i => i.Product);

    public async Task<OrderResponseDto> CreateOrderAsync(Guid userId, OrderCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ShippingAddress))
        {
            throw new ArgumentException("Shipping address is required!", nameof(dto));
        }

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
            .FirstOrDefaultAsync(c => c.UserId == userId);

        var activeItems = cart?.Items.Where(i => !i.Deleted && i.Quantity > 0).ToList() ?? [];
        if (activeItems.Count == 0)
        {
            throw new InvalidOperationException("Cannot create an order with an empty cart!");
        }

        var now = DateTime.UtcNow;
        var productRepo = unitOfWork.GetRepository<Product>();
        var cartItemRepo = unitOfWork.GetRepository<CartItem>();

        var order = new Order
        {
            OrderNumber =
                $"ORD-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            ShippingAddress = dto.ShippingAddress.Trim(),
            Status = OrderStatus.Pending,
            UserId = userId,
        };

        decimal subTotal = 0;
        decimal totalTax = 0;

        foreach (var cartItem in activeItems)
        {
            var product = cartItem.Product;
            if (product == null || product.Deleted || !product.Active)
            {
                throw new InvalidOperationException(
                    $"Product '{product?.Name ?? cartItem.ProductId.ToString()}' is no longer available!"
                );
            }

            if (product.StockQuantity < cartItem.Quantity)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for '{product.Name}'. Available: {product.StockQuantity}, requested: {cartItem.Quantity}."
                );
            }

            product.StockQuantity -= cartItem.Quantity;
            product.UpdatedAt = now;
            productRepo.Update(product);

            var discountedUnitPrice = product.DiscountedPrice;
            var lineTax = (product.FinalPriceWithTax - discountedUnitPrice) * cartItem.Quantity;

            order.Items.Add(
                new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = cartItem.Quantity,
                    OriginalUnitPrice = product.Price,
                    DiscountRate = product.DiscountRate,
                    UnitPrice = discountedUnitPrice,
                    TaxRate = product.TaxRate,
                    TaxAmount = lineTax,
                }
            );

            subTotal += discountedUnitPrice * cartItem.Quantity;
            totalTax += lineTax;

            cartItem.Deleted = true;
            cartItem.Active = false;
            cartItem.UpdatedAt = now;
            cartItemRepo.Update(cartItem);
        }

        order.SubTotal = subTotal;
        order.TotalTax = totalTax;
        order.TotalAmount = subTotal + totalTax;

        cart!.UpdatedAt = now;
        cartRepo.Update(cart);

        await unitOfWork.GetRepository<Order>().CreateAsync(order);
        var reply = await unitOfWork.CommitAsync();

        if (!reply.IsSuccess)
        {
            throw new InvalidOperationException(string.Join("; ", reply.Errors ?? []));
        }

        return (await GetByIdAsync(order.Id))!;
    }

    public async Task<OrderResponseDto?> GetByIdAsync(Guid id)
    {
        var order = await BuildOrderQuery().FirstOrDefaultAsync(o => o.Id == id);
        return order == null ? null : mapper.Map<OrderResponseDto>(order);
    }

    public async Task<OrderResponseDto?> GetByOrderNumberAsync(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            return null;
        }

        var normalizedLower = orderNumber.Trim().ToLower();
        var order = await BuildOrderQuery()
            .FirstOrDefaultAsync(o => o.OrderNumber.ToLower() == normalizedLower);

        return order == null ? null : mapper.Map<OrderResponseDto>(order);
    }

    public async Task<IEnumerable<OrderResponseDto>> GetUserOrdersAsync(Guid userId)
    {
        var orders = await BuildOrderQuery()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return mapper.Map<IEnumerable<OrderResponseDto>>(orders);
    }

    public async Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync()
    {
        var orders = await BuildOrderQuery().OrderByDescending(o => o.CreatedAt).ToListAsync();

        return mapper.Map<IEnumerable<OrderResponseDto>>(orders);
    }

    public async Task<Reply<OrderResponseDto>> UpdateOrderStatusAsync(
        Guid orderId,
        OrderStatus status
    )
    {
        var orderRepo = unitOfWork.GetRepository<Order>();
        var order = await BuildOrderQuery(enableTracking: true)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return Reply<OrderResponseDto>.Fail("Order not found!");
        }

        if (order.Status == status)
        {
            return Reply<OrderResponseDto>.Success(mapper.Map<OrderResponseDto>(order));
        }

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Delivered)
        {
            return Reply<OrderResponseDto>.Fail(
                $"Order in '{order.Status}' status cannot be updated!"
            );
        }

        if (order.Status == OrderStatus.Shipped && status != OrderStatus.Delivered)
        {
            return Reply<OrderResponseDto>.Fail(
                $"A shipped order can only transition to '{OrderStatus.Delivered}'!"
            );
        }

        var now = DateTime.UtcNow;
        if (status == OrderStatus.Cancelled)
        {
            var productRepo = unitOfWork.GetRepository<Product>();
            foreach (var item in order.Items)
            {
                if (item.Product != null)
                {
                    item.Product.StockQuantity += item.Quantity;
                    item.Product.UpdatedAt = now;
                    productRepo.Update(item.Product);
                }
            }
        }

        order.Status = status;
        order.UpdatedAt = now;
        orderRepo.Update(order);

        var reply = await unitOfWork.CommitAsync();
        if (!reply.IsSuccess)
        {
            return Reply<OrderResponseDto>.Fail(reply.Errors!);
        }

        var updated = await GetByIdAsync(order.Id);
        return Reply<OrderResponseDto>.Success(updated ?? mapper.Map<OrderResponseDto>(order));
    }
}
