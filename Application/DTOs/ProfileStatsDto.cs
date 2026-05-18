namespace Application.DTOs;

public class ProfileStatsDto
{
    public bool IsAdmin { get; set; }
    
    // Métricas para Vendedores
    public int MyMonthlySalesCount { get; set; }
    public decimal MyMonthlySalesAmount { get; set; }
    public decimal MyDailySalesAmount { get; set; }
    public decimal MyAverageTicket { get; set; }
    
    // Métricas para Administradores
    public decimal SystemMonthlyRevenue { get; set; }
    public int SystemLowStockCount { get; set; }
    public int SystemNewCustomersCount { get; set; }
    public int SystemActiveUsersCount { get; set; }
}
