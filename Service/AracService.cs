using KiralamaAPI.Data;
using KiralamaAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text.Json;
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

		public async Task<Arac> AracEkle(AracEkleDto dto, Guid isletmeId)
		{
			Console.WriteLine($"AracEkle çağrıldı - dto: {JsonSerializer.Serialize(dto)}, isletmeId: {isletmeId}");

			var plakaMevcut = await _context.Araclar.AnyAsync(a => a.PlakaNumarasi == dto.PlakaNumarasi);
			Console.WriteLine($"PlakaMevcut: {plakaMevcut}");
			if (plakaMevcut)
				throw new InvalidOperationException("Bu plaka numarasına sahip bir araç zaten mevcut.");

			var isletmeMevcut = await _context.Isletmeler.AnyAsync(i => i.Id == isletmeId);
			Console.WriteLine($"IsletmeMevcut: {isletmeMevcut}");
			if (!isletmeMevcut)
				throw new InvalidOperationException("Belirtilen işletme bulunamadı.");

			Console.WriteLine($"SaatlikUcret Kontrolü: {dto.SaatlikUcret}");
			if (dto.SaatlikUcret <= 0)
				throw new InvalidOperationException("Saatlik ücret pozitif bir değer olmalıdır.");

			Console.WriteLine($"KonumEnlem: {dto.KonumEnlem}, KonumBoylam: {dto.KonumBoylam}");
			if (dto.KonumEnlem.HasValue && (dto.KonumEnlem < -90 || dto.KonumEnlem > 90))
				throw new InvalidOperationException("Geçersiz enlem değeri (-90 ile 90 arasında olmalı).");

			if (dto.KonumBoylam.HasValue && (dto.KonumBoylam < -180 || dto.KonumBoylam > 180))
				throw new InvalidOperationException("Geçersiz boylam değeri (-180 ile 180 arasında olmalı).");

			var yeniArac = new Arac
			{
				Id = Guid.NewGuid(),
				PlakaNumarasi = dto.PlakaNumarasi,
				Model = dto.Model,
				Tip = dto.Tip,
				SaatlikUcret = dto.SaatlikUcret,
				KonumEnlem = dto.KonumEnlem,
				KonumBoylam = dto.KonumBoylam,
				MusaitMi = true,
				Kilitli = true,
				IsletmeId = isletmeId
			};
			Console.WriteLine($"Yeni Araç Oluşturuldu: {JsonSerializer.Serialize(yeniArac)}");

			try
			{
				_context.Araclar.Add(yeniArac);
				Console.WriteLine("Veritabanına araç eklendi, SaveChangesAsync çağrılıyor...");
				await _context.SaveChangesAsync();
				Console.WriteLine("Araç başarıyla kaydedildi.");
				return yeniArac;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Hata: {ex.Message}\nInnerException: {ex.InnerException?.Message}\nStackTrace: {ex.StackTrace}");
				throw new InvalidOperationException($"Araç eklenemedi: {ex.Message}");
			}
		}

		
		public async Task<Arac> AracGetir(Guid aracId)
		{
			var arac = await _context.Araclar
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
				return false;
			}

			arac.Ac();
			await _context.SaveChangesAsync();

			return true;
		}

		public async Task<Arac> AracGuncelle(Guid id, AracGuncelleDto dto)
		{
			var mevcutArac = await _context.Araclar.FindAsync(id);
			if (mevcutArac == null)
			{
				Console.WriteLine($"Araç bulunamadı, ID: {id}");
				return null;
			}

			if (dto.SaatlikUcret <= 0)
				throw new InvalidOperationException("Saatlik ücret pozitif bir değer olmalıdır.");

			if (dto.KonumEnlem.HasValue && (dto.KonumEnlem < -90 || dto.KonumEnlem > 90))
				throw new InvalidOperationException("Geçersiz enlem değeri (-90 ile 90 arasında olmalı).");

			if (dto.KonumBoylam.HasValue && (dto.KonumBoylam < -180 || dto.KonumBoylam > 180))
				throw new InvalidOperationException("Geçersiz boylam değeri (-180 ile 180 arasında olmalı).");

			// Mevcut aracı güncelle
			mevcutArac.PlakaNumarasi = dto.PlakaNumarasi;
			mevcutArac.Model = dto.Model;
			mevcutArac.Tip = dto.Tip;
			mevcutArac.SaatlikUcret = dto.SaatlikUcret;
			mevcutArac.KonumEnlem = dto.KonumEnlem;
			mevcutArac.KonumBoylam = dto.KonumBoylam;
			mevcutArac.MusaitMi = dto.MusaitMi;

			try
			{
				await _context.SaveChangesAsync();
				Console.WriteLine($"Araç başarıyla güncellendi, ID: {id}");
				return mevcutArac;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Hata: {ex.Message}\nInnerException: {ex.InnerException?.Message}\nStackTrace: {ex.StackTrace}");
				throw new InvalidOperationException($"Araç güncellenemedi: {ex.Message}");
			}
		}

		public async Task<Arac> AracSil(Guid id)
		{
			var arac = await _context.Araclar.FindAsync(id);
			if (arac == null)
			{
				Console.WriteLine($"Araç bulunamadı, ID: {id}");
				return null;
			}

			// Kiralama kontrolü
			var kiralamaVar = await _context.Kiralamalar.AnyAsync(k => k.AracId == id && k.Durum == "Aktif");
			if (kiralamaVar)
			{
				Console.WriteLine($"Araç silinemedi: Araç aktif bir kiralamada, ID: {id}");
				throw new InvalidOperationException("Araç aktif bir kiralamada olduğu için silinemez.");
			}

			try
			{
				_context.Araclar.Remove(arac);
				Console.WriteLine($"Araç veritabanından silindi, ID: {id}");
				await _context.SaveChangesAsync();
				Console.WriteLine($"Veritabanı değişikliği kaydedildi, ID: {id}");
				return arac;
			}
			catch (DbUpdateException ex)
			{
				Console.WriteLine($"Veritabanı Hatası: {ex.Message}\nInnerException: {ex.InnerException?.Message}\nStackTrace: {ex.StackTrace}");
				throw new InvalidOperationException($"Araç silinemedi: {ex.InnerException?.Message}");
			}
		}

		public async Task<List<Arac>> AraclariListele(Guid isletmeId)
		{
			return await _context.Araclar
				.Include(a => a.Isletme)
				.Where(a => a.IsletmeId == isletmeId)
				.ToListAsync();
		}

		public async Task<List<Arac>> GetNearbyAraclar(double enlem, double boylam, double mesafeKm)
		{
			double dereceFarkiEnlem = mesafeKm / 111.0;
			double dereceFarkiBoylam = mesafeKm / (111.0 * Math.Cos(ToRadians(enlem)));
			var yakinAraclar = await _context.Araclar
				.Where(a => a.KonumEnlem.HasValue && a.KonumBoylam.HasValue &&
							a.KonumEnlem >= enlem - dereceFarkiEnlem && a.KonumEnlem <= enlem + dereceFarkiEnlem &&
							a.KonumBoylam >= boylam - dereceFarkiBoylam && a.KonumBoylam <= boylam + dereceFarkiBoylam)
				.ToListAsync();

			return yakinAraclar
				.Where(a => GetDistanceInKm(enlem, boylam, a.KonumEnlem.Value, a.KonumBoylam.Value) <= mesafeKm)
				.ToList();
		}

		private double GetDistanceInKm(double lat1, double lon1, double lat2, double lon2)
		{
			var R = 6371;
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

		public async Task<bool> KiralamaBitir(Guid aracId)
		{
			var arac = await _context.Araclar.FirstOrDefaultAsync(a => a.Id == aracId);
			if (arac == null)
			{
				return false;
			}

			arac.MusaitMi = true; // Aracı müsait hale getir
			try
			{
				await _context.SaveChangesAsync();
				Console.WriteLine($"Araç müsait hale getirildi, ID: {aracId}");
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Hata: {ex.Message}\nStackTrace: {ex.StackTrace}");
				throw new InvalidOperationException($"Araç durumu güncellenemedi: {ex.Message}");
			}
		}

		private double ToRadians(double deg)
		{
			return deg * (Math.PI / 180);
		}
	}
}