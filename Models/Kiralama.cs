using System.ComponentModel.DataAnnotations.Schema;

namespace KiralamaAPI.Models
{
	public class Kiralama
	{
		public Guid Id { get; set; } = Guid.NewGuid();

		[ForeignKey("Kullanici")]
		public Guid KullaniciId { get; set; }
		public Kullanici Kullanici { get; set; }

		[ForeignKey("Arac")]
		public Guid AracId { get; set; }
		public Arac Arac { get; set; }

		public DateTime BaslangicTarihi { get; set; }
		public DateTime? BitisTarihi { get; set; }

		public decimal? Ucret { get; set; }

		public string Durum { get; set; } // "Aktif" veya "Tamamlandı"
	}
}
