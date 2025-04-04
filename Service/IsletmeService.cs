using KiralamaAPI.Data;
using KiralamaAPI.Models;
using Microsoft.EntityFrameworkCore; 
using System.Collections.Generic;
using System.Threading.Tasks;


namespace KiralamaAPI.Service
{
	public class IsletmeService : IIsletmeService
	{
		private readonly KiralamaDbContext _context;

		public IsletmeService(KiralamaDbContext context)
		{
			_context = context;
		}

		public async Task<Isletme> IsletmeEkle(Isletme isletme)
		{
			_context.Isletmeler.Add(isletme);
			await _context.SaveChangesAsync();
			return isletme;
		}

		public async Task<Isletme> IsletmeGuncelle(Guid id, Isletme isletme)
		{
			var mevcut = await _context.Isletmeler.FindAsync(id);
			if (mevcut == null)
				return null;

			mevcut.Ad = isletme.Ad;
			mevcut.Eposta = isletme.Eposta;
			mevcut.SifreHash = isletme.SifreHash;

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

		public async Task<List<Isletme>> IsletmeleriListele()
		{
			return await _context.Isletmeler.ToListAsync();
		}

		public async Task<Isletme> IsletmeGetir(Guid id)
		{
			return await _context.Isletmeler.FindAsync(id);
		}


	}
}
