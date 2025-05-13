using System.ComponentModel.DataAnnotations;

namespace KiralamaAPI.Models
{
	public class IsletmeKayitDto
	{
		public string Ad { get; set; } 
		public string Eposta { get; set; }
		public string Sifre { get; set; }

		[MaxLength(200)] 
		public string Adres { get; set; }
	}
}
