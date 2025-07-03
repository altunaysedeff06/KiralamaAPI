using System.ComponentModel.DataAnnotations;

namespace KiralamaAPI.Models
{
	public class AracEkleDto
	{
		[MaxLength(50)]
		public string? PlakaNumarasi { get; set; }

		[Required, MaxLength(100)]
		public string Model { get; set; }

		[Required, MaxLength(50)]
		public string Tip { get; set; }

		[Required]
		public decimal SaatlikUcret { get; set; }

		public double? KonumEnlem { get; set; }
		public double? KonumBoylam { get; set; }

		
	}
}
