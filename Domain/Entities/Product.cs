using Domain.ValueObjects;

namespace Domain.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; private set; } = string.Empty;
    public Price UnitPrice { get; private set; }
    public int Stock { get; private set; }

    protected Product() { }

    public Product(string name, Price unitPrice, int stock)
    {
        Name = name;
        UnitPrice = unitPrice;
        Stock = stock;
    }

    public void DecreaseStock (int Amount)
    {
        if (Amount <= 0)
            throw new ArgumentException("La cantidad a descontar debe ser mayor a cero ");

        if (Amount > Stock)
            throw new ArgumentException($"Stock insuficiente para el producto '{Name}'. Disponible '{Stock}'. Solicitado '{Amount}'.");

        Stock -= Amount;


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
