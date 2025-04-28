using KiralamaAPI.Models;

namespace KiralamaAPI.Service
{
	public interface IKullaniciService
	{
		Task<Kullanici> KayitOl(Kullanici kullanici);
		Task<Kullanici> GirisYap(KullaniciGirisModel girisModel);
		Task<Kullanici> Guncelle(Guid id, Kullanici kullanici);
		Task<Kullanici?> ProfilGetirAsync(Guid kullaniciId);
		Task<bool> KullaniciGuncelleAsync(Kullanici kullanici);
		Task<bool> Sil(Guid id);
	}
}
