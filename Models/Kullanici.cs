using System.ComponentModel.DataAnnotations;

namespace KiralamaAPI.Models
{
	public class Kullanici
	{
		public Guid Id { get; set; }

		[MaxLength(100)]
		public string? Ad { get; set; }

		[MaxLength(100)]
		public string? Soyad { get; set; }

		[Required, EmailAddress]
		public string Eposta { get; set; }

		[Required]
		[MinLength(6)]
		public string SifreHash { get; set; } // Şifre hash olarak tutulacak

		public DateTime KayitTarihi { get; set; } = DateTime.UtcNow;
		public string Rol { get; set; }

	}
}
