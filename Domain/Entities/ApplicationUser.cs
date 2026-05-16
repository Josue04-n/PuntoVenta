using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public class ApplicationUser : IdentityUser<int>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? IDCard { get; set; }
    public bool IsActive { get; set; } = true;

    // Auditoría básica para el usuario
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
