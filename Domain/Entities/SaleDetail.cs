using Domain.ValueObjects;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class SaleDetail
{
    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public int Amount { get; private set; }
    public Price UnitPrice { get; private set; }
    public virtual Product Product { get; set; }
    [ForeignKey("ProductId")]
    public Price SubTotal => UnitPrice * Amount;

    private SaleDetail() { }

    public SaleDetail(int productId, int amount, Price unitPrice)
    {
        ProductId = productId;
        Amount = amount;
        UnitPrice = unitPrice;
    }

}
