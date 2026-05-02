namespace Application.DTOs;

public class CustomerResponseDto
{
    public int Id { get; set; }
    public string IDCard { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName => $"{LastName}{Name}";
}
