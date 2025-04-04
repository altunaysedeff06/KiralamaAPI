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
			kiralama.Durum = "Aktif";
			kiralama.BaslangicTarihi = DateTime.UtcNow;
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

		public async Task<List<Kiralama>> KullaniciKiralamaGecmisi(Guid kullaniciId)
		{
			return await _context.Kiralamalar
				.Where(k => k.KullaniciId == kullaniciId)
				.Include(k => k.Arac)
				.OrderByDescending(k => k.BaslangicTarihi)
				.ToListAsync();
		}

		public async Task<bool> KiralamaDurumGuncelle(Guid kiralamaId, string yeniDurum)
		{
			var kiralama = await _context.Kiralamalar.FindAsync(kiralamaId);
			if (kiralama == null) return false;

			kiralama.Durum = yeniDurum;

			if (yeniDurum == "Tamamlandı")
				kiralama.BitisTarihi = DateTime.UtcNow;

			await _context.SaveChangesAsync();
			return true;
		}
	}
}
