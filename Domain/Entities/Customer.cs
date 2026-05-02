namespace Domain.Entities;

public class Customer
{
    public int Id { get; set; }
    public string IDCard { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    public bool IsValid => !string.IsNullOrWhiteSpace(IDCard) && IDCard.Length <= 10;
    private Customer() { }

}
