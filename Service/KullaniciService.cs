using KiralamaAPI.Data;
using KiralamaAPI.Models;
using Microsoft.EntityFrameworkCore; 
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;


namespace KiralamaAPI.Service
{
	public class KullaniciService : IKullaniciService
	{
		private readonly KiralamaDbContext _context;

		public KullaniciService(KiralamaDbContext context)
		{
			_context = context;
		}

		public async Task<Kullanici> KayitOl(Kullanici kullanici)
		{
			kullanici.SifreHash = HashSifre(kullanici.SifreHash);
			_context.Kullanicilar.Add(kullanici);
			await _context.SaveChangesAsync();
			return kullanici;
		}

		public async Task<Kullanici> GirisYap(KullaniciGirisModel girisModel)
		{
			var hashliSifre = HashSifre(girisModel.Sifre);
			return await _context.Kullanicilar
				.FirstOrDefaultAsync(k => k.Eposta == girisModel.Eposta && k.SifreHash == hashliSifre);
		}

		public async Task<Kullanici> Guncelle(Guid id, Kullanici guncelKullanici)
		{
			var mevcut = await _context.Kullanicilar.FindAsync(id);
			if (mevcut == null) return null;

			mevcut.Ad = guncelKullanici.Ad;
			mevcut.Soyad = guncelKullanici.Soyad;
			mevcut.Eposta = guncelKullanici.Eposta;

			// Şifre değiştiyse güncelle
			if (!string.IsNullOrEmpty(guncelKullanici.SifreHash))
				mevcut.SifreHash = HashSifre(guncelKullanici.SifreHash);

			await _context.SaveChangesAsync();
			return mevcut;
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
			using var sha256 = SHA256.Create();
			var bytes = Encoding.UTF8.GetBytes(sifre);
			var hash = sha256.ComputeHash(bytes);
			return Convert.ToBase64String(hash);
		}
	}
}
