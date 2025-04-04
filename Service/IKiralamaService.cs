using KiralamaAPI.Models;

namespace KiralamaAPI.Service
{
	public interface IKiralamaService
	{
		Task<Kiralama> KiralamaBaslat(Kiralama kiralama);
		Task<Kiralama?> KiralamaGetir(Guid id);
		Task<List<Kiralama>> KullaniciKiralamaGecmisi(Guid kullaniciId);
		Task<bool> KiralamaDurumGuncelle(Guid kiralamaId, string yeniDurum);
	}
}
