using Core.Abstracts.Bases;
using Core.Concretes.Enums;

namespace Core.Concretes.Entities;

public class OrderItem : BaseEntity
{
    public int Quantity { get; set; }
    public decimal OriginalUnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
}

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

