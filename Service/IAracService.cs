using KiralamaAPI.Models;
using System.Threading.Tasks;

namespace KiralamaAPI.Service
{
	public interface IAracService
	{
		Task<Arac> AracGetir(Guid aracId);
		Task<bool> Kilitle(Guid aracId);
		Task<bool> Ac(Guid aracId);
		Task<Arac> AracEkle(AracEkleDto dto, Guid isletmeId);
		Task<Arac> AracGuncelle(Guid id, AracGuncelleDto dto);
		Task<bool> KiralamaBitir(Guid aracId);
		Task<Arac> AracSil(Guid id);
		Task<List<Arac>> AraclariListele(Guid isletmeId);
		Task<List<Arac>> GetNearbyAraclar(double enlem, double boylam, double mesafeKm);
	}
}
