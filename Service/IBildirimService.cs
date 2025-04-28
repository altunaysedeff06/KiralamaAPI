using KiralamaAPI.Models;

namespace KiralamaAPI.Service
{
	public interface IBildirimService
	{
		Task SendNotificationAsync(Guid userId, string message);
		Task<List<Bildirim>> GetUserNotificationsAsync(Guid userId);
	}
}

