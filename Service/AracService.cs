using KiralamaAPI.Data;
using KiralamaAPI.Models;
using Microsoft.EntityFrameworkCore; // Bunu mutlaka ekle
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace KiralamaAPI.Service
{
	public class AracService : IAracService
	{
		private readonly KiralamaDbContext _context;

		public AracService(KiralamaDbContext context)
		{
			_context = context;
		}

		public async Task<Arac> AracGetir(Guid aracId)
		{
			var arac = await _context.Araclar
				.Include(a => a.User)
				.FirstOrDefaultAsync(a => a.Id == aracId);
			return arac;
		}

		public async Task<bool> Kilitle(Guid aracId)
		{
			var arac = await _context.Araclar.FirstOrDefaultAsync(a => a.Id == aracId);

			if (arac == null)
			{
				return false; 
			}

			arac.Kilitle(); 
			await _context.SaveChangesAsync();

			return true; 
		}

		public async Task<bool> Ac(Guid aracId)
		{
			var arac = await _context.Araclar.FirstOrDefaultAsync(a => a.Id == aracId);

			if (arac == null)
			{
				return false; // Araç bulunamadı
			}

			arac.Ac(); // Bu metodu Arac sınıfında tanımladınız
			await _context.SaveChangesAsync();

			return true; // Araç başarıyla açıldı
		}

		public async Task<Arac> AracEkle(Arac arac)
		{
			_context.Araclar.Add(arac);
			await _context.SaveChangesAsync();
			return arac;
		}

		public async Task<Arac> AracGuncelle(int id, Arac arac)
		{
			var mevcutArac = await _context.Araclar.FindAsync(id);
			if (mevcutArac == null)
			{
				return null;
			}
			mevcutArac.PlakaNumarasi = arac.PlakaNumarasi;
			mevcutArac.Model = arac.Model;
			mevcutArac.Tip = arac.Tip;
			mevcutArac.SaatlikUcret = arac.SaatlikUcret;
			mevcutArac.KonumEnlem = arac.KonumEnlem;
			mevcutArac.KonumBoylam = arac.KonumBoylam;
			mevcutArac.MusaitMi = arac.MusaitMi;

			await _context.SaveChangesAsync();
			return mevcutArac;
		}

		public async Task<Arac> AracSil(int id)
		{
			var arac = await _context.Araclar.FindAsync(id);
			if (arac == null) return null;

			_context.Araclar.Remove(arac);
			await _context.SaveChangesAsync();
			return arac;
		}

		public async Task<List<Arac>> AraclariListele()
		{
			return await _context.Araclar.ToListAsync();
		}


		//KONUM İŞLEMLERİ
		public async Task<List<Arac>> GetNearbyAraclar(double enlem, double boylam, double mesafeKm)
		{
			// Asenkron veri çekme işlemi (örneğin, veritabanı sorgusu)
			var araclar = await _context.Araclar
				.Where(a => GetDistanceInKm(enlem, boylam, a.KonumEnlem, a.KonumBoylam) <= mesafeKm)
				.ToListAsync();

			return araclar;
		}


		// Haversine formülü ile iki koordinat arasındaki mesafeyi hesaplama
		private double GetDistanceInKm(double lat1, double lon1, double lat2, double lon2)
		{
			var R = 6371; // Dünya yarıçapı (km)
			var dLat = ToRadians(lat2 - lat1);
			var dLon = ToRadians(lon2 - lon1);
			var a =
				Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
				Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
				Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
			var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
			var distance = R * c;
			return distance;
		}

		private double ToRadians(double deg)
		{
			return deg * (Math.PI / 180);
		}

	}
}
