using KiralamaAPI.Data;
using KiralamaAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KiralamaAPI.Service
{
	// DTO sınıfı: Sadece e-posta ve şifre için
	public class KullaniciKayitDto
	{
		public string Eposta { get; set; }
		public string Sifre { get; set; }
	}

	public class KullaniciService : IKullaniciService
	{
		private readonly KiralamaDbContext _context;

		public KullaniciService(KiralamaDbContext context)
		{
			_context = context;
		}

		public async Task<Kullanici?> ProfilGetirAsync(Guid kullaniciId)
		{
			return await _context.Kullanicilar
				.AsNoTracking()
				.FirstOrDefaultAsync(k => k.Id == kullaniciId);
		}

		public async Task<Kullanici> KayitOl(KullaniciKayitDto kayitDto)
		{
			// Giriş doğrulaması
			if (string.IsNullOrWhiteSpace(kayitDto.Eposta) || string.IsNullOrWhiteSpace(kayitDto.Sifre))
			{
				throw new ArgumentException("E-posta ve şifre alanları zorunludur.");
			}

			// E-posta zaten kayıtlı mı?
			if (await _context.Kullanicilar.AnyAsync(k => k.Eposta == kayitDto.Eposta))
			{
				throw new InvalidOperationException("Bu e-posta adresi zaten kayıtlı.");
			}

			var yeniKullanici = new Kullanici
			{
				Eposta = kayitDto.Eposta,
				SifreHash = HashSifre(kayitDto.Sifre),
				KayitTarihi = DateTime.Now,
				Rol = "Kullanici" // Varsayılan rol
			};

			_context.Kullanicilar.Add(yeniKullanici);
			await _context.SaveChangesAsync();

			return yeniKullanici;
		}

		public async Task<Kullanici?> GirisYap(KullaniciKayitDto kayitDto)
		{
			if (kayitDto == null || string.IsNullOrWhiteSpace(kayitDto.Eposta) || string.IsNullOrWhiteSpace(kayitDto.Sifre))
			{
				Console.WriteLine("Giriş hatası: E-posta veya şifre boş.");
				return null;
			}

			var hashliSifre = HashSifre(kayitDto.Sifre);
			Console.WriteLine($"Giriş denemesi: Eposta={kayitDto.Eposta}, HashliSifre={hashliSifre}");

			var kullanici = await _context.Kullanicilar
				.AsNoTracking()
				.FirstOrDefaultAsync(k =>
					k.Eposta == kayitDto.Eposta &&
					k.SifreHash == hashliSifre
				);

			if (kullanici == null)
			{
				Console.WriteLine($"Kullanıcı bulunamadı: Eposta={kayitDto.Eposta}");
			}
			else
			{
				Console.WriteLine($"Kullanıcı bulundu: ID={kullanici.Id}, Rol={kullanici.Rol}");
			}

			return kullanici;
		}

		public async Task<Kullanici> Guncelle(Guid id, Kullanici guncelKullanici)
		{
			var mevcut = await _context.Kullanicilar.FindAsync(id);
			if (mevcut == null) return null;

			mevcut.Ad = guncelKullanici.Ad;
			mevcut.Soyad = guncelKullanici.Soyad;
			mevcut.Eposta = guncelKullanici.Eposta;

			if (!string.IsNullOrEmpty(guncelKullanici.SifreHash))
				mevcut.SifreHash = HashSifre(guncelKullanici.SifreHash);

			await _context.SaveChangesAsync();
			return mevcut;
		}

		public async Task<bool> KullaniciGuncelleAsync(Kullanici kullanici)
		{
			var mevcutKullanici = await _context.Kullanicilar.FindAsync(kullanici.Id);
			if (mevcutKullanici == null)
				return false;

			mevcutKullanici.Rol = kullanici.Rol;
			_context.Kullanicilar.Update(mevcutKullanici);
			await _context.SaveChangesAsync();

			return true;
		}

		public async Task<bool> Sil(Guid id)
		{
			var kullanici = await _context.Kullanicilar.FindAsync(id);
			if (kullanici == null) return false;

			_context.Kullanicilar.Remove(kullanici);
			await _context.SaveChangesAsync();
			return true;
		}

		private string HashSifre(string sifre)
		{
			if (string.IsNullOrEmpty(sifre))
				throw new ArgumentNullException(nameof(sifre), "Şifre boş olamaz.");

			using var sha256 = SHA256.Create();
			var bytes = Encoding.UTF8.GetBytes(sifre);
			var hash = sha256.ComputeHash(bytes);
			return Convert.ToBase64String(hash);
		}
	}
}