using KiralamaAPI.Data;
using KiralamaAPI.Models;
using Microsoft.EntityFrameworkCore; 
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;


namespace KiralamaAPI.Service
{
	public class IsletmeService : IIsletmeService
	{
		private readonly KiralamaDbContext _context;

		public IsletmeService(KiralamaDbContext context)
		{
			_context = context;
		}

		public async Task<Isletme> IsletmeEkle(IsletmeKayitDto kayitDto)
		{
			var mevcutAd = await _context.Isletmeler.AnyAsync(i => i.Ad == kayitDto.Ad);
			if (mevcutAd)
				throw new InvalidOperationException("Bu işletme adı zaten kullanılıyor.");

			var mevcutEposta = await _context.Isletmeler.AnyAsync(i => i.Eposta == kayitDto.Eposta);
			if (mevcutEposta)
				throw new InvalidOperationException("Bu e-posta adresi zaten kullanılıyor.");

			var yeniIsletme = new Isletme
			{
				Id = Guid.NewGuid(),
				Ad = kayitDto.Ad,
				Eposta = kayitDto.Eposta,
				Adres = kayitDto.Adres ?? "", // Adres null ise boş string ata
				KayitTarihi = DateTime.Now,
				Rol = "Isletme"
			};

			yeniIsletme.SifreHash = HashSifre(kayitDto.Sifre, yeniIsletme.Id);

			_context.Isletmeler.Add(yeniIsletme);
			await _context.SaveChangesAsync();
			return yeniIsletme;
		}

		public async Task<Isletme> IsletmeGuncelle(Guid id, IsletmeGuncelleDto guncelleDto)
		{
			Console.WriteLine($"Güncelleme denemesi: ID={id}, Eposta={guncelleDto.Eposta}");
			var mevcut = await _context.Isletmeler.FindAsync(id);
			if (mevcut == null)
			{
				Console.WriteLine("Isletme bulunamadı.");
				return null;
			}

			// Benzersizlik kontrolü
			if (!string.IsNullOrEmpty(guncelleDto.Ad) && mevcut.Ad != guncelleDto.Ad)
			{
				var adMevcut = await _context.Isletmeler
					.AnyAsync(i => i.Ad == guncelleDto.Ad && i.Id != id);
				if (adMevcut)
					return null;
			}

			if (!string.IsNullOrEmpty(guncelleDto.Eposta) && mevcut.Eposta != guncelleDto.Eposta)
			{
				var epostaMevcut = await _context.Isletmeler
					.AnyAsync(i => i.Eposta == guncelleDto.Eposta && i.Id != id);
				if (epostaMevcut)
					return null;
			}

			mevcut.Ad = guncelleDto.Ad ?? mevcut.Ad; // Null ise mevcut değeri koru
			mevcut.Adres = guncelleDto.Adres ?? mevcut.Adres; // Null ise mevcut değeri koru
			mevcut.Eposta = guncelleDto.Eposta ?? mevcut.Eposta; // Null ise mevcut değeri koru

			// Şifre güncelleniyorsa
			if (!string.IsNullOrWhiteSpace(guncelleDto.Sifre))
			{
				mevcut.SifreHash = HashSifre(guncelleDto.Sifre, mevcut.Id);
			}

			await _context.SaveChangesAsync();
			return mevcut;
		}

		public async Task<Isletme> IsletmeSil(Guid id)
		{
			var isletme = await _context.Isletmeler.FindAsync(id);
			if (isletme == null)
				return null;

			_context.Isletmeler.Remove(isletme);
			await _context.SaveChangesAsync();
			return isletme;
		}

		public async Task<Isletme> IsletmeGetir(Guid id)
		{
			return await _context.Isletmeler.FindAsync(id);
		}

		public async Task<Isletme> GirisYap(IsletmeGirisDto girisDto)
		{
			if (girisDto == null || string.IsNullOrWhiteSpace(girisDto.Eposta) || string.IsNullOrWhiteSpace(girisDto.Sifre))
				return null;

			var isletme = await _context.Isletmeler
				.AsNoTracking()
				.FirstOrDefaultAsync(i => i.Eposta == girisDto.Eposta);

			if (isletme == null || !SifreDogrula(girisDto.Sifre, isletme.SifreHash, isletme.Id))
				return null;

			return isletme;
		}

		public string HashSifre(string sifre, Guid isletmeId)
		{
			if (string.IsNullOrEmpty(sifre))
				throw new ArgumentNullException(nameof(sifre), "Şifre boş olamaz.");

			// Şifre ve isletmeId'yi birleştirip hash'le
			using var sha256 = SHA256.Create();
			var sifreVeTuz = Encoding.UTF8.GetBytes(sifre + isletmeId.ToString());
			var hash = sha256.ComputeHash(sifreVeTuz);
			return Convert.ToBase64String(hash);
		}

		public bool SifreDogrula(string sifre, string hash, Guid isletmeId)
		{
			if (string.IsNullOrEmpty(sifre) || string.IsNullOrEmpty(hash))
				return false;

			// Aynı isletmeId ile şifreyi tekrar hash'le
			var yeniHash = HashSifre(sifre, isletmeId);
			return yeniHash == hash;
		}


	}
}
