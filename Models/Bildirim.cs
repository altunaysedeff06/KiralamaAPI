namespace KiralamaAPI.Models
{
	public class Bildirim
	{
		public Guid Id { get; set; }
		public Guid UserId { get; set; } 
		public string Message { get; set; } 
		public DateTime DateSent { get; set; } 
		public bool IsRead { get; set; } 
	}
}
