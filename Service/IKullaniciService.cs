using KiralamaAPI.Models;

namespace KiralamaAPI.Service
{
	public interface IKullaniciService
	{
		Task<Kullanici> KayitOl(KullaniciKayitDto kayitDto);
		Task<Kullanici> GirisYap(KullaniciKayitDto kayitDto);
		Task<Kullanici> Guncelle(Guid id, Kullanici kullanici);
		Task<Kullanici?> ProfilGetirAsync(Guid kullaniciId);
		Task<bool> KullaniciGuncelleAsync(Kullanici kullanici);


		Task<List<Kullanici>> TumKullanicilariGetir();
		Task<bool> Sil(Guid id);
	}
}
