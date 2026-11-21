using Core.Abstracts.Bases;

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
    public Product Product { get; set; } = null!;

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
}