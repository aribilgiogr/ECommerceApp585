using Core.Abstracts.Bases;
using Core.Concretes.Enums;

namespace Core.Concretes.Entities;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = null!;
    public decimal SubTotal { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string ShippingAddress { get; set; } = null!;

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
}