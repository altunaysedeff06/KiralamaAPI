using System.ComponentModel.DataAnnotations;

namespace KiralamaAPI.Models
{
	public class Isletme
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		[Required, MaxLength(200)]
		public string Ad { get; set; }

		[MaxLength(200)]
		public string Adres { get; set; }

		[Required, EmailAddress]
		public string Eposta { get; set; }

		[Required]
		public string SifreHash { get; set; }

		[Required]
		public string Rol { get; set; } = "Isletme"; 

		public DateTime KayitTarihi { get; set; } = DateTime.Now;
	}
}
