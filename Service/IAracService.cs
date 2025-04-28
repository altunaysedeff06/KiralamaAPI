using KiralamaAPI.Models;

namespace KiralamaAPI.Service
{
	public interface IAracService
	{
		Task<Arac> AracEkle(Arac arac);
		Task<Arac> AracGuncelle(int id, Arac arac);
		Task<Arac> AracSil(int id);
		Task<List<Arac>> AraclariListele();
		Task<List<Arac>> GetNearbyAraclar(double enlem, double boylam, double mesafeKm);
		Task<Arac> AracGetir(Guid aracId);
		Task<bool> Kilitle(Guid aracId);
		Task<bool> Ac(Guid aracId);
	}
}
