using System.ComponentModel.DataAnnotations;

namespace KiralamaAPI.Models
{
	public class IsletmeGuncelleDto
	{
		[MaxLength(200)]
		public string Ad { get; set; }

		[MaxLength(200)]
		public string Adres { get; set; }

		[Required, EmailAddress]
		public string Eposta { get; set; }

		public string Sifre { get; set; }
	}
}
