using KiralamaAPI.Data;
using KiralamaAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace KiralamaAPI.Service
{
	public class BildirimService : IBildirimService
	{
		private readonly KiralamaDbContext _context;

		public BildirimService(KiralamaDbContext context)
		{
			_context = context;
		}

		public async Task SendNotificationAsync(Guid userId, string message)
		{
			var notification = new Bildirim
			{
				UserId = userId,
				Message = message,
				DateSent = DateTime.UtcNow,
				IsRead = false
			};

			await _context.Bildirimler.AddAsync(notification);
			await _context.SaveChangesAsync();
		}

		public async Task<List<Bildirim>> GetUserNotificationsAsync(Guid userId)
		{
			return await _context.Bildirimler
				.Where(n => n.UserId == userId)
				.OrderByDescending(n => n.DateSent)
				.ToListAsync(); 
		}
	}
}
