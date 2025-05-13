using System.ComponentModel.DataAnnotations;
namespace KiralamaAPI.Models
{
	public class IsletmeGirisDto
	{
		[Required, EmailAddress]
		public string Eposta { get; set; }

		[Required]
		public string Sifre { get; set; }
	}
}
