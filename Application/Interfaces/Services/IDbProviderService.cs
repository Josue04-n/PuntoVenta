namespace Application.Interfaces.Services;

public interface IDbProviderService
{
    string ProviderName { get; }
    Task<int> GetNextInvoiceSequenceAsync();
    // Aquí podemos añadir más métodos específicos si Oracle requiere algo especial para los 100k registros
}
