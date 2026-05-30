using Application.DTOs;

namespace Application.Interfaces.Services;

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int? userId, string role, string? userName = null);
}
