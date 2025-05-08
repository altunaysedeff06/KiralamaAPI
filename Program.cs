using KiralamaAPI.Data;
using KiralamaAPI.Models;
using KiralamaAPI.Service;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.ComponentModel.DataAnnotations;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
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

			// JWT AUTHENTICATION EKLENDÝ
			builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
				.AddJwtBearer(options =>
				{
					options.TokenValidationParameters = new TokenValidationParameters
					{
						ValidateIssuer = true,
						ValidateAudience = true,
						ValidateLifetime = true,
						ValidateIssuerSigningKey = true,
						ValidIssuer = builder.Configuration["Jwt:Issuer"],
						ValidAudience = builder.Configuration["Jwt:Audience"],
						IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
					};
				});

			// Servisleri DI konteynerine ekle
			builder.Services.AddScoped<IAracService, AracService>();
			builder.Services.AddScoped<IIsletmeService, IsletmeService>();
			builder.Services.AddScoped<IKullaniciService, KullaniciService>();
			builder.Services.AddScoped<IKiralamaService, KiralamaService>();
			builder.Services.AddScoped<IBildirimService, BildirimService>();


			builder.Services.AddControllers(); // (Gerekli deðilse kaldýrabilirsin)
											   //builder.Services.AddAuthorization();
			builder.Services.AddAuthorization(options =>
			{
				options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
				options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
			});



			// Swagger/OpenAPI yapýlandýrmasý
			builder.Services.AddEndpointsApiExplorer();
			//builder.Services.AddSwaggerGen();
			builder.Services.AddSwaggerGen(c =>
			{
					c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
					{
						In = ParameterLocation.Header,
						Description = "Please enter JWT with Bearer into field",
						Name = "Authorization",
						Type = SecuritySchemeType.ApiKey
					});

					c.AddSecurityRequirement(new OpenApiSecurityRequirement
					{
						{
							new OpenApiSecurityScheme
							{
								Reference = new OpenApiReference
								{
									Type = ReferenceType.SecurityScheme,
									Id = "Bearer"
								}
							},
							new string[] {}
							}
						});
					});


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


			app.MapPost("/kullanici/kayit", async (IKullaniciService kullaniciService, KullaniciKayitDto kayitDto) =>
			{
				// Verinin doðruluðunu manuel kontrol et
				var validationResults = new List<ValidationResult>();
				var validationContext = new ValidationContext(kayitDto);
				bool isValid = Validator.TryValidateObject(kayitDto, validationContext, validationResults, true);

				if (!isValid)
				{
					return Results.BadRequest(validationResults);
				}

				var yeniKullanici = await kullaniciService.KayitOl(kayitDto);
				return Results.Ok(yeniKullanici);
			});



			app.MapPost("/kullanici/giris", async (IKullaniciService kullaniciService, KullaniciKayitDto kayitDto) =>
			{
				var kullanici = await kullaniciService.GirisYap(kayitDto);

				if (kullanici == null)
					return Results.Unauthorized(); // Eðer kullanýcý doðrulanmazsa, 401 döner

				// Kullanýcý doðrulandý, þimdi JWT token oluþturuyoruz
				var claims = new[]
				{
					new Claim(ClaimTypes.Name, kullanici.Id.ToString()),  // Kullanýcý ID'si
					new Claim(ClaimTypes.Role, kullanici.Rol)  // Kullanýcýnýn rolü
				};

				var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("BuBenimSuperGucluKeyimBuBenimSuperGucluKeyimBuBenimSuperGucluKeyimBuBenimSuperGucluKeyimBuBenimSuperGucluKeyimBuBenimSuperGucluKeyim"));
				var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

				var token = new JwtSecurityToken(
					issuer: "KiralamaAPI",  // Token vereni
					audience: "KiralamaAPIUsers",  // Token'ý hedef alan
					claims: claims,  // Claims, yani token'da saklanacak bilgileri
					expires: DateTime.Now.AddMinutes(60),  // Token'ýn geçerlilik süresi
					signingCredentials: credentials  // Güvenli imzalama anahtarý
				);

				// Token'ý oluþturduktan sonra geri döndür
				var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

				return Results.Ok(new { Token = tokenString });  // JWT token'ý döner
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

			app.MapGet("/kullanici/profil", async (HttpContext http, IKullaniciService service) =>
			{
				var kullaniciId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (string.IsNullOrEmpty(kullaniciId))
					return Results.Unauthorized(); // önce kontrol et

				var kullaniciGuid = Guid.Parse(kullaniciId); // ondan sonra parse yap
				var kullanici = await service.ProfilGetirAsync(kullaniciGuid);

				return kullanici == null ? Results.NotFound() : Results.Ok(kullanici);
			});

			app.MapPost("/araclar/nearby", async (LocationRequest request, IAracService aracService) =>
			{
				var araclar = await aracService.GetNearbyAraclar(request.Enlem, request.Boylam, request.MesafeKm);

				if (araclar == null || araclar.Count == 0)
					return Results.NotFound("Yakýnlarda araç bulunamadý.");

				return Results.Ok(araclar);
			});



			app.MapPost("/admin/ekle", async (IKullaniciService kullaniciService, HttpContext httpContext) =>
			{
				// Kullanýcýnýn kimliðini kontrol et
				var kullaniciId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (string.IsNullOrEmpty(kullaniciId))
					return Results.Unauthorized();

				var kullaniciGuid = Guid.Parse(kullaniciId);  // Kimlikten kullanýcý id'sini al

				// Kullanýcýyý servisten getir
				var kullanici = await kullaniciService.ProfilGetirAsync(kullaniciGuid);
				if (kullanici == null)
					return Results.NotFound("Kullanýcý bulunamadý.");

				// Admin rolü olup olmadýðýný kontrol et
				if (kullanici.Rol != "Admin")
					return Results.Forbid();  // Eðer admin deðilse, eriþim reddedilir.

				// Admin ekleme iþlemi
				// Burada kullanýcýyý admin yapmak için mevcut kullanýcýyý güncelliyoruz
				kullanici.Rol = "Admin"; // Kullanýcýnýn rolünü admin olarak deðiþtiriyoruz

				// Kullanýcýyý güncellemek
				var result = await kullaniciService.KullaniciGuncelleAsync(kullanici);  // Kullanýcý güncelleme metodunu çaðýrabilirsiniz

				if (result)
					return Results.Ok("Kullanýcý baþarýyla admin olarak güncellendi.");
				else
					return Results.BadRequest("Kullanýcý admin olarak güncellenemedi.");
			})
			.RequireAuthorization();

			app.MapGet("/arac/{id}/kullanici", async (Guid id, IAracService aracService) =>
			{
				var arac = await aracService.AracGetir(id);
				if (arac == null)
					return Results.NotFound("Araç ve kullanýcý bilgisi bulunamadý.");

				// Aracýn ve kullanýcýnýn bilgilerini döndürüyoruz
				var aracKullanici = new
				{
					arac.Model,
					arac.User.Id,
					arac.User.Ad,
					arac.User.Eposta
				};

				return Results.Ok(aracKullanici);
			});



			app.MapPost("/arac/{id}/kilitle", async (Guid id, IAracService aracService, IBildirimService bildirimService) =>
			{
				var arac = await aracService.AracGetir(id);
				if (arac == null)
					return Results.NotFound("Araç bulunamadý.");

				// Kilitleme iþlemi
				var success = await aracService.Kilitle(id);

				if (success)
				{
					await bildirimService.SendNotificationAsync(arac.UserId, "Araç baþarýyla kilitlendi.");
					return Results.Ok("Araç baþarýyla kilitlendi.");
				}

				return Results.BadRequest("Kilitleme iþlemi baþarýsýz.");
			});





			app.Run();
		}
	}
}
