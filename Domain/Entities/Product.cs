using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Entities;

public class Product : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; private set; } = string.Empty;
    public Price UnitPrice { get; private set; }
    public int Stock { get; private set; }

    protected Product() { }

    public Product(string name, Price unitPrice, int stock)
    {
        Update(name, unitPrice, stock);
    }

    public void Update(string name, Price unitPrice, int stock)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del producto es obligatorio.");
        
        if (stock < 0)
            throw new ArgumentException("El stock no puede ser negativo.");

        Name = ToTitleCase(name);
        UnitPrice = unitPrice;
        Stock = stock;
    }

    private string ToTitleCase(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        text = text.Trim();
        if (text.Length <= 1) return text.ToUpper();
        return char.ToUpper(text[0]) + text.Substring(1).ToLower();
    }

    public void DecreaseStock (int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("La cantidad a descontar debe ser mayor a cero ");

        if (amount > Stock)
            throw new ArgumentException($"Stock insuficiente para el producto '{Name}'. Disponible '{Stock}'. Solicitado '{amount}'.");

        Stock -= amount;


    }

    public void RemoveStock(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("La cantidad a retirar debe ser mayor a cero."); 

        if (Stock < amount)
            throw new InvalidOperationException($"Stock insuficiente para {Name}."); 

        // Modificamos el estado internamente (Encapsulación)
        Stock -= amount;
    }

}
