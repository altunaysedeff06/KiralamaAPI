using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace KiralamaAPI.Models
{
	public class Arac
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		[Required, MaxLength(50)]
		public string PlakaNumarasi { get; set; }

		[Required, MaxLength(100)]
		public string Model { get; set; }

		[Required, MaxLength(50)]
		public string Tip { get; set; } // Araba, motosiklet, scooter vb.

		[Required]
		public decimal SaatlikUcret { get; set; }

		public double KonumEnlem { get; set; }
		public double KonumBoylam { get; set; }

		public bool MusaitMi { get; set; } = true;

		[ForeignKey("Isletme")]
		public Guid IsletmeId { get; set; }
		public Isletme Isletme { get; set; }
	}
}
