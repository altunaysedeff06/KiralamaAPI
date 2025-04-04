using KiralamaAPI.Data;
using KiralamaAPI.Models;
using KiralamaAPI.Service;
using Microsoft.EntityFrameworkCore;

namespace KiralamaAPI
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Connection String'i oku ve DbContext'i ekle
			var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

			builder.Services.AddDbContext<KiralamaDbContext>(options =>
				options.UseSqlServer(connectionString));

			// Servisleri DI konteynerine ekle
			builder.Services.AddScoped<IAracService, AracService>();
			builder.Services.AddScoped<IIsletmeService, IsletmeService>();
			builder.Services.AddScoped<IKullaniciService, KullaniciService>();
			builder.Services.AddScoped<IKiralamaService, KiralamaService>();


			builder.Services.AddControllers(); // (Gerekli deðilse kaldýrabilirsin)
			builder.Services.AddAuthorization();

			// Swagger/OpenAPI yapýlandýrmasý
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			var app = builder.Build();

			// HTTP Request Pipeline yapýlandýrmasý
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();
			app.UseAuthorization();

			// Minimal API endpoint'leri
			app.MapPost("/isletme/arac/ekle", async (IAracService aracService, Arac arac) =>
			{
				var eklenenArac = await aracService.AracEkle(arac);
				return Results.Ok(eklenenArac);
			});

			app.MapPut("/isletme/arac/guncelle/{id}", async (int id, IAracService aracService, Arac arac) =>
			{
				var guncellenenArac = await aracService.AracGuncelle(id, arac);
				if (guncellenenArac == null)
					return Results.NotFound("Araç bulunamadý.");
				return Results.Ok(guncellenenArac);
			});

			app.MapDelete("/isletme/arac/sil/{id}", async (int id, IAracService aracService) =>
			{
				var silinenArac = await aracService.AracSil(id);
				if (silinenArac == null)
					return Results.NotFound("Araç bulunamadý.");
				return Results.Ok("Araç baþarýyla silindi.");
			});

			app.MapGet("/isletme/araclar", async (IAracService aracService) =>
			{
				var araclar = await aracService.AraclariListele();
				return Results.Ok(araclar);
			});


			app.MapPost("/isletme/ekle", async (IIsletmeService service, Isletme isletme) =>
			{
				var eklenen = await service.IsletmeEkle(isletme);
				return Results.Ok(eklenen);
			});

			app.MapPut("/isletme/guncelle/{id}", async (Guid id, IIsletmeService service, Isletme isletme) =>
			{
				var guncel = await service.IsletmeGuncelle(id, isletme);
				return guncel == null ? Results.NotFound() : Results.Ok(guncel);
			});

			app.MapDelete("/isletme/sil/{id}", async (Guid id, IIsletmeService service) =>
			{
				var silinen = await service.IsletmeSil(id);
				return silinen == null ? Results.NotFound() : Results.Ok("Ýþletme silindi");
			});

			app.MapGet("/isletme/liste", async (IIsletmeService service) =>
			{
				var liste = await service.IsletmeleriListele();
				return Results.Ok(liste);
			});

			app.MapGet("/isletme/{id}", async (Guid id, IIsletmeService service) =>
			{
				var isletme = await service.IsletmeGetir(id);
				return isletme == null ? Results.NotFound() : Results.Ok(isletme);
			});


			app.MapPost("/kullanici/kayit", async (IKullaniciService kullaniciService, Kullanici kullanici) =>
			{
				var yeniKullanici = await kullaniciService.KayitOl(kullanici);
				return Results.Ok(yeniKullanici);
			});

			app.MapPost("/kullanici/giris", async (IKullaniciService kullaniciService, KullaniciGirisModel model) =>
			{
				var kullanici = await kullaniciService.GirisYap(model);
				if (kullanici == null)
					return Results.Unauthorized();
				return Results.Ok(kullanici);
			});

			app.MapPut("/kullanici/guncelle/{id}", async (Guid id, Kullanici guncelKullanici, IKullaniciService service) =>
			{
				var sonuc = await service.Guncelle(id, guncelKullanici);
				if (sonuc == null)
					return Results.NotFound("Kullanýcý bulunamadý.");
				return Results.Ok(sonuc);
			});

			app.MapDelete("/kullanici/sil/{id}", async (Guid id, IKullaniciService service) =>
			{
				var silindi = await service.Sil(id);
				return silindi ? Results.Ok("Hesap silindi.") : Results.NotFound("Kullanýcý bulunamadý.");
			});


			app.MapPost("/kiralama/baslat", async (IKiralamaService service, Kiralama kiralama) =>
			{
				var sonuc = await service.KiralamaBaslat(kiralama);
				return Results.Ok(sonuc);
			});

			app.MapGet("/kiralama/{id}", async (IKiralamaService service, Guid id) =>
			{
				var kiralama = await service.KiralamaGetir(id);
				return kiralama == null ? Results.NotFound() : Results.Ok(kiralama);
			});

			app.MapGet("/kiralama/kullanici/{kullaniciId}", async (IKiralamaService service, Guid kullaniciId) =>
			{
				var kiralamalar = await service.KullaniciKiralamaGecmisi(kullaniciId);
				return Results.Ok(kiralamalar);
			});

			app.MapPut("/kiralama/durum-guncelle/{id}", async (IKiralamaService service, Guid id, string durum) =>
			{
				var basarili = await service.KiralamaDurumGuncelle(id, durum);
				return basarili ? Results.Ok("Durum güncellendi.") : Results.NotFound("Kiralama bulunamadý.");
			});


			// Eðer controller kullanýlmýyorsa bu satýra gerek yok:
			// app.MapControllers();

			app.Run();
		}
	}
}
