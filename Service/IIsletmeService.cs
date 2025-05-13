using KiralamaAPI.Models;

namespace KiralamaAPI.Service
{
	public interface IIsletmeService
	{
		Task<Isletme> IsletmeEkle(IsletmeKayitDto kayitDto);
		Task<Isletme> IsletmeGuncelle(Guid id, IsletmeGuncelleDto guncelleDto);
		Task<Isletme> IsletmeSil(Guid id);
		Task<Isletme> IsletmeGetir(Guid id);
		Task<Isletme> GirisYap(IsletmeGirisDto girisDto);
		string HashSifre(string sifre, Guid isletmeId);
		bool SifreDogrula(string sifre, string hash, Guid isletmeId);
	}
}
