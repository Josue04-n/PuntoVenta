using Application.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int? userId, string role)
    {
        var notifications = new List<NotificationDto>();

        // 1. Notificaciones de Bajo Stock (Para todos)
        var lowStockProducts = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.Stock <= 5)
            .Take(10)
            .ToListAsync();

        foreach (var p in lowStockProducts)
        {
            bool isOut = p.Stock == 0;
            notifications.Add(new NotificationDto
            {
                Message = isOut ? $"¡Atención! '{p.Name}' se ha agotado por completo (0 unidades)." : $"Stock bajo: '{p.Name}' tiene solo {p.Stock} unidades.",
                Type = isOut ? "Danger" : "Warning",
                Icon = isOut ? "bi-x-octagon-fill" : "bi-exclamation-triangle-fill",
                CreatedAt = p.UpdatedAt ?? p.CreatedAt,
                TargetUrl = "/productos"
            });
        }

        // 2. Notificaciones de Ventas (Solo para Administradores)
        if (role == "Administrador")
        {
            // Tomar las últimas 5 ventas de las últimas 24 horas
            var recentSales = await _context.Sales
                .Include(s => s.Customer)
                .AsNoTracking()
                .OrderByDescending(s => s.IssueDate)
                .Take(5)
                .ToListAsync();

            foreach (var s in recentSales)
            {
                notifications.Add(new NotificationDto
                {
                    Message = $"Nueva Venta: {s.InvoiceNumber} por ${s.Total:N2} (Cliente: {s.Customer?.LastName ?? "CF"})",
                    Type = "Success",
                    Icon = "bi-cart-check-fill",
                    CreatedAt = s.IssueDate,
                    TargetUrl = $"/facturas?numeroFactura={s.InvoiceNumber}"
                });
            }
        }

        return notifications.OrderByDescending(n => n.CreatedAt);
    }
}
