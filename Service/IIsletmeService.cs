using KiralamaAPI.Models;

namespace KiralamaAPI.Service
{
	public interface IIsletmeService
	{
		Task<Isletme> IsletmeEkle(Isletme isletme);
		Task<Isletme> IsletmeGuncelle(Guid id, Isletme isletme);
		Task<Isletme> IsletmeSil(Guid id);
		Task<List<Isletme>> IsletmeleriListele();
		Task<Isletme> IsletmeGetir(Guid id);
	}
}
