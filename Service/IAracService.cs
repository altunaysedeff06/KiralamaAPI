using KiralamaAPI.Models;

namespace KiralamaAPI.Service
{
	public interface IAracService
	{
		Task<Arac> AracEkle(Arac arac);
		Task<Arac> AracGuncelle(int id, Arac arac);
		Task<Arac> AracSil(int id);
		Task<List<Arac>> AraclariListele();
	}
}
