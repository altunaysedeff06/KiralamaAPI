using KiralamaAPI.Data;
using KiralamaAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KiralamaAPI.Service
{
	public class KiralamaService : IKiralamaService
	{
		private readonly KiralamaDbContext _context;

		public KiralamaService(KiralamaDbContext context)
		{
			_context = context;
		}

		public async Task<Kiralama> KiralamaBaslat(Kiralama kiralama)
		{
			// Kullanıcının aktif kiralama sayısını kontrol et (maksimum 1 ile sınırla)
			var aktifKiralamaSayisi = await _context.Kiralamalar
				.CountAsync(k => k.KullaniciId == kiralama.KullaniciId && k.Durum == "Aktif");
			if (aktifKiralamaSayisi > 0)
				throw new InvalidOperationException("Zaten aktif bir kiralamanız var.");

			kiralama.Durum = "Aktif";
			kiralama.BaslangicTarihi = DateTime.Now;
			await _context.Kiralamalar.AddAsync(kiralama);
			await _context.SaveChangesAsync();
			return kiralama;
		}

		public async Task<Kiralama?> KiralamaGetir(Guid id)
		{
			return await _context.Kiralamalar
				.Include(k => k.Arac)
				.Include(k => k.Kullanici)
				.FirstOrDefaultAsync(k => k.Id == id);
		}

		public async Task<Kiralama?> KiralamaGetirByAracId(Guid aracId)
		{
			return await _context.Kiralamalar
				.Include(k => k.Arac)
				.FirstOrDefaultAsync(k => k.AracId == aracId && k.Durum == "Aktif");
		}

		public async Task<List<Kiralama>> KullaniciKiralamaGecmisi(Guid kullaniciId)
		{
			return await _context.Kiralamalar
				.Where(k => k.KullaniciId == kullaniciId)
				.Include(k => k.Arac)
				.OrderByDescending(k => k.BaslangicTarihi)
				.ToListAsync();
		}

		public async Task<bool> KiralamaBitir(Guid kiralamaId)
		{
			var kiralama = await _context.Kiralamalar.FindAsync(kiralamaId);
			if (kiralama == null)
			{
				return false;
			}

			kiralama.Durum = "Pasif"; 
			kiralama.BitisTarihi = DateTime.UtcNow; 
			try
			{
				await _context.SaveChangesAsync();
				Console.WriteLine($"Kiralama pasif hale getirildi ve bitiş tarihi eklendi, ID: {kiralamaId}");
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Hata: {ex.Message}\nStackTrace: {ex.StackTrace}");
				throw new InvalidOperationException($"Kiralama durumu güncellenemedi: {ex.Message}");
			}
		}

		public async Task<bool> KiralamaDurumGuncelle(Guid kiralamaId, string yeniDurum)
		{
			var kiralama = await _context.Kiralamalar.FindAsync(kiralamaId);
			if (kiralama == null) return false;

			kiralama.Durum = yeniDurum;

			if (yeniDurum == "Tamamlandı")
			{
				kiralama.BitisTarihi = DateTime.UtcNow;
				// Ücret hesaplama (örnek)
				var arac = await _context.Araclar.FindAsync(kiralama.AracId);
				if (arac != null)
				{
					var saatFarki = (kiralama.BitisTarihi - kiralama.BaslangicTarihi)?.TotalHours ?? 0;
					kiralama.Ucret = (decimal)(saatFarki * (double)arac.SaatlikUcret);
				}
			}

			await _context.SaveChangesAsync();
			return true;
		}
	}
}
