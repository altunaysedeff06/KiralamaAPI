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
	}
}
