using System;
using System.Collections.Generic;

using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using KiralamaAPI.Data;
using KiralamaAPI.Models;
using KiralamaAPI.Service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Authentication.JwtBearer; 
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;


namespace KiralamaAPI;

public class Program
{
	public static void Main(string[] args)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
		string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
		builder.Services.AddDbContext<KiralamaDbContext>(options =>
						options.UseSqlServer(connectionString));


		builder.Services.AddAuthentication(options =>
		{
			options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
			options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
		})
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
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
		RoleClaimType = ClaimTypes.Role,
		NameClaimType = ClaimTypes.NameIdentifier,
		RequireSignedTokens = true // Token'�n imzal� oldu�undan emin ol
	};
	options.Events = new JwtBearerEvents
	{
		OnAuthenticationFailed = context =>
		{
			Console.WriteLine($"JWT Do�rulama Hatas�: {context.Exception.Message}, Token: {context.Request.Headers["Authorization"]}, InnerException: {context.Exception.InnerException?.Message}");
			return Task.CompletedTask;
		},
		OnTokenValidated = context =>
		{
			Console.WriteLine("JWT Token Do�ruland�. Kullan�c�: {context.Principal?.Identity?.Name}");
			return Task.CompletedTask;
		},
		OnMessageReceived = context =>
		{
			Console.WriteLine($"Gelen Token: {context.Token}");
			return Task.CompletedTask;
		}
	};
});


		builder.Services.AddScoped<IAracService, AracService>();
		builder.Services.AddScoped<IIsletmeService, IsletmeService>();
		builder.Services.AddScoped<IKullaniciService, KullaniciService>();
		builder.Services.AddScoped<IKiralamaService, KiralamaService>();
		
		builder.Services.AddControllers();
		builder.Services.AddCors(options =>
		{
			options.AddDefaultPolicy(
				policy =>
				{
					policy.WithOrigins("http://localhost:5063")
						  .AllowAnyHeader()   
						  .AllowAnyMethod();  
				});
		});

		builder.Services.AddAuthorization(delegate (AuthorizationOptions options)
		{
			options.AddPolicy("AdminOnly", delegate (AuthorizationPolicyBuilder policy)
			{
				policy.RequireRole("Admin");
			});
			options.AddPolicy("UserOnly", delegate (AuthorizationPolicyBuilder policy)
			{
				policy.RequireRole("User");
			});
			options.AddPolicy("IsletmeOnly", policy =>
			policy.RequireClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "Isletme"));
		});
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen(c =>
		{
			c.SwaggerDoc("v1", new OpenApiInfo { Title = "KiralamaAPI", Version = "v1" });
			c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
			{
				In = ParameterLocation.Header,
				Description = "Please enter JWT with Bearer into field (e.g., 'Bearer {token}')",
				Name = "Authorization",
				Type = SecuritySchemeType.Http,
				Scheme = "bearer",
				BearerFormat = "JWT"
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
					new string[] { }
				}
			});
		});
		WebApplication app = builder.Build();
		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
		}
		app.UseCors();
		app.UseAuthentication();
		app.UseAuthorization();

		var apiKey = builder.Configuration["ApiKey"];

		app.MapPost("/isletme/arac/ekle", async (IAracService aracService, AracEkleDto aracDto, HttpContext context) =>
		{
			try
			{
				var isletmeId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (string.IsNullOrEmpty(isletmeId))
					return Results.Unauthorized();

				var yeniArac = await aracService.AracEkle(aracDto, Guid.Parse(isletmeId));
				return Results.Ok(yeniArac);
			}
			catch (InvalidOperationException ex)
			{
				return Results.BadRequest(new { Message = ex.Message });
			}
			catch (Exception ex)
			{
				return Results.Json(
					new { Message = "Bir hata olu�tu: " + ex.Message },
					statusCode: 500
				);
			}
		}).RequireAuthorization("IsletmeOnly");
		app.MapPut("/isletme/arac/guncelle/{id}", async (Guid id, IAracService aracService, AracGuncelleDto dto, HttpContext context) =>
		{
			try
			{
				var isletmeId = Guid.Parse(context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
				var mevcutArac = await aracService.AracGetir(id);
				if (mevcutArac == null)
				{
					Console.WriteLine($"Ara� bulunamad�, ID: {id}");
					return Results.NotFound(new { Message = $"Ara� bulunamad�. ID: {id}" });
				}
				if (mevcutArac.IsletmeId != isletmeId)
				{
					Console.WriteLine($"Yetkisiz eri�im, ID: {id}, ��letme ID: {isletmeId}");
					return Results.Forbid();
				}
				var guncellenenArac = await aracService.AracGuncelle(id, dto);
				if (guncellenenArac == null)
				{
					return Results.NotFound(new { Message = "Ara� bulunamad�." });
				}
				return Results.Ok(guncellenenArac);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Hata: {ex.Message}");
				return Results.Json(new { Message = "Ara� g�ncelleme ba�ar�s�z: " + ex.Message }, statusCode: 500);
			}
		}).RequireAuthorization("IsletmeOnly");

		app.MapDelete("/isletme/arac/sil/{id}", async (Guid id, IAracService aracService) =>
		{
			try
			{
				var silinenArac = await aracService.AracSil(id);
				if (silinenArac == null)
				{
					Console.WriteLine($"Ara� silme ba�ar�s�z: Ara� bulunamad�, ID: {id}");
					return Results.NotFound(new { Message = "Ara� bulunamad�." });
				}
				Console.WriteLine($"Ara� ba�ar�yla silindi, ID: {id}");
				return Results.Ok(new { Message = "Ara� ba�ar�yla silindi." });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Hata: {ex.Message}\nInnerException: {ex.InnerException?.Message}\nStackTrace: {ex.StackTrace}");
				return Results.Json(new { Message = "Ara� silme ba�ar�s�z: " + ex.Message }, statusCode: 500);
			}
		}).RequireAuthorization("IsletmeOnly");

		app.MapGet("/isletme/araclar/{isletmeId}", async (Guid isletmeId, IAracService aracService) =>
			Results.Ok<List<Arac>>(await aracService.AraclariListele(isletmeId)))
		.RequireAuthorization("IsletmeOnly");

		app.MapPost("/arac/{id}/ac", async (Guid id, IAracService aracService, HttpContext context) =>
		{
			var providedApiKey = context.Request.Headers["X-Api-Key"].ToString();
			if (string.IsNullOrEmpty(providedApiKey) || providedApiKey != apiKey)
				return Results.Json(new { Message = "Ge�ersiz veya eksik API anahtar�." }, statusCode: StatusCodes.Status401Unauthorized);

			var arac = await aracService.AracGetir(id);
			if (arac == null)
				return Results.NotFound(new { Message = "Ara� bulunamad�." });

			if (!await aracService.Ac(id))
				return Results.BadRequest(new { Message = "A�ma i�lemi ba�ar�s�z. Ara� kilitli veya ba�ka bir sorun var." });

			return Results.Ok(new { Message = "Ara� ba�ar�yla a��ld�." });
		});

		app.MapPost("/arac/{id}/kilitle", async (Guid id, IAracService aracService, HttpContext context) =>
		{
			var providedApiKey = context.Request.Headers["X-Api-Key"].ToString();
			if (string.IsNullOrEmpty(providedApiKey) || providedApiKey != apiKey)
				return Results.Json(new { Message = "Ge�ersiz veya eksik API anahtar�." }, statusCode: StatusCodes.Status401Unauthorized);

			var arac = await aracService.AracGetir(id);
			if (arac == null)
				return Results.NotFound(new { Message = "Ara� bulunamad�." });

			if (await aracService.Kilitle(id))
				return Results.Ok(new { Message = "Ara� ba�ar�yla kilitlendi." });

			return Results.BadRequest(new { Message = "Kilitleme i�lemi ba�ar�s�z." });
		});


		app.MapPost("/arac/guncelle_konum", async (IAracService aracService, AracKonumGuncelleDto dto, HttpContext context) =>
		{
			var providedApiKey = context.Request.Headers["X-Api-Key"].ToString();
			if (string.IsNullOrEmpty(providedApiKey) || providedApiKey != apiKey)
				return Results.Json(new { Message = "Ge�ersiz veya eksik API anahtar�." }, statusCode: StatusCodes.Status401Unauthorized);

			try
			{
				var arac = await aracService.AracGetir(dto.AracId);
				if (arac == null)
					return Results.NotFound(new { Message = "Ara� bulunamad�." });

				var guncellenenArac = await aracService.AracGuncelle(dto.AracId, new AracGuncelleDto
				{
					PlakaNumarasi = arac.PlakaNumarasi,
					Model = arac.Model,
					Tip = arac.Tip,
					SaatlikUcret = arac.SaatlikUcret,
					KonumEnlem = dto.Enlem,
					KonumBoylam = dto.Boylam,
					MusaitMi = arac.MusaitMi
				});
				return Results.Ok(guncellenenArac);
			}
			catch (Exception ex)
			{
				return Results.Json(new { Message = "Konum g�ncelleme ba�ar�s�z: " + ex.Message }, statusCode: 500);
			}
		});

		app.MapGet("/kiralama/arac_durum", async (IKiralamaService kiralamaService, Guid aracId) =>
		{
			try
			{
				var kiralama = await kiralamaService.KiralamaGetirByAracId(aracId);
				if (kiralama == null)
					return Results.Ok("Pasif");
				return Results.Ok(kiralama.Durum);
			}
			catch (Exception ex)
			{
				return Results.Json(new { Message = "Kiralama durumu al�namad�: " + ex.Message }, statusCode: 500);
			}
		});

		app.MapPut("/kiralama/bitir/{id}", async (Guid id, IKiralamaService kiralamaService, IAracService aracService, HttpContext context) =>
		{
			try
			{
				var kullaniciId = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;
				if (string.IsNullOrEmpty(kullaniciId))
					return Results.Unauthorized();

				var kiralama = await kiralamaService.KiralamaGetir(id);
				if (kiralama == null)
					return Results.NotFound(new { Message = "Kiralama bulunamad�." });
				if (kiralama.KullaniciId.ToString() != kullaniciId)
					return Results.Forbid();

				// Kiralama durumunu g�ncelle
				var basarili = await kiralamaService.KiralamaDurumGuncelle(id, "Tamamland�");
				if (!basarili)
					return Results.BadRequest(new { Message = "Kiralama durumu g�ncellenemedi." });

				// Arac� m�sait yap ve kilitle
				var arac = await aracService.AracGetir(kiralama.AracId);
				if (arac != null)
				{
					await aracService.AracGuncelle(arac.Id, new AracGuncelleDto
					{
						PlakaNumarasi = arac.PlakaNumarasi,
						Model = arac.Model,
						Tip = arac.Tip,
						SaatlikUcret = arac.SaatlikUcret,
						KonumEnlem = arac.KonumEnlem,
						KonumBoylam = arac.KonumBoylam,
						MusaitMi = true
					});
					await aracService.Kilitle(arac.Id);
				}

				return Results.Ok(new { Message = "Kiralama ba�ar�yla tamamland�." });
			}
			catch (Exception ex)
			{
				return Results.Json(new { Message = "Kiralama bitirme ba�ar�s�z: " + ex.Message }, statusCode: 500);
			}
		}).RequireAuthorization();




		app.MapPost("/isletme/kayit", (Func<IIsletmeService, IsletmeKayitDto, Task<IResult>>)async delegate (IIsletmeService isletmeService, IsletmeKayitDto kayitDto)
		{
			try
			{
				if (kayitDto == null || string.IsNullOrWhiteSpace(kayitDto.Ad) || string.IsNullOrWhiteSpace(kayitDto.Eposta) || string.IsNullOrWhiteSpace(kayitDto.Sifre))
				{
					return Results.BadRequest(new
					{
						Message = "��letme ad�, e-posta veya �ifre bo� olamaz."
					});
				}
				return Results.Ok<Isletme>(await isletmeService.IsletmeEkle(kayitDto));
			}
			catch (InvalidOperationException ex7)
			{
				InvalidOperationException ex5 = ex7;
				return Results.Conflict(new { ex5.Message });
			}
			catch (DbUpdateException ex8)
			{
				DbUpdateException ex6 = ex8;
				return Results.BadRequest(new
				{
					Message = "Veritaban� hatas�: " + ex6.InnerException?.Message
				});
			}
		});


		app.MapPost("/isletme/giris", async (IIsletmeService isletmeService, IsletmeGirisDto girisDto) =>
		{
			var loginResponse = await isletmeService.GirisYap(girisDto);
			if (loginResponse == null || loginResponse.Isletme == null)
				return Results.Unauthorized();

			var claims = new[]
			{
		new Claim(ClaimTypes.NameIdentifier, loginResponse.Isletme.Id.ToString()),
		new Claim(ClaimTypes.Role, "Isletme")
	};

			var jwtKey = builder.Configuration["Jwt:Key"];
			if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 16)
			{
				Console.WriteLine("Hata: Jwt:Key appsettings.json'da bo� veya �ok k�sa (en az 16 byte olmal�).");
				return Results.BadRequest(new { Message = "Sunucu yap�land�rma hatas�." });
			}

			var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
			var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				issuer: builder.Configuration["Jwt:Issuer"],
				audience: builder.Configuration["Jwt:Audience"],
				claims: claims,
				notBefore: DateTime.UtcNow,
				expires: DateTime.UtcNow.AddMinutes(builder.Configuration.GetValue("Jwt:ExpireMinutes", 60)),
				signingCredentials: credentials
			);

			var tokenHandler = new JwtSecurityTokenHandler();
			var tokenString = tokenHandler.WriteToken(token);

			if (string.IsNullOrEmpty(tokenString))
			{
				Console.WriteLine("Hata: Token olu�turulamad�. L�tfen Jwt yap�land�rmas�n� kontrol edin.");
				return Results.BadRequest(new { Message = "Token olu�turma hatas�." });
			}

			Console.WriteLine($"Olu�turulan Token: {tokenString}");
			return Results.Ok(new
			{
				Token = tokenString,
				Isletme = loginResponse.Isletme
			});
		});




		app.MapDelete("/isletme/sil/{id}",	async (Guid id, IIsletmeService service) =>	(await service.IsletmeSil(id) == null)
			? Results.NotFound((object)null)
			: Results.Ok<string>("��letme silindi"));

		app.MapGet("/isletme/{id}", (Func<Guid, IIsletmeService, Task<IResult>>)async delegate (Guid id, IIsletmeService service)
		{
			Isletme isletme = await service.IsletmeGetir(id);
			return (isletme == null) ? Results.NotFound((object)null) : Results.Ok<Isletme>(isletme);
		}).RequireAuthorization("IsletmeOnly");
		app.MapPost("/kullanici/kayit", (Func<IKullaniciService, KullaniciKayitDto, Task<IResult>>)async delegate (IKullaniciService kullaniciService, KullaniciKayitDto kayitDto)
		{
			List<ValidationResult> validationResults = new List<ValidationResult>();
			ValidationContext validationContext = new ValidationContext(kayitDto);
			return (!Validator.TryValidateObject(kayitDto, validationContext, validationResults, validateAllProperties: true)) ? Results.BadRequest<List<ValidationResult>>(validationResults) : Results.Ok<Kullanici>(await kullaniciService.KayitOl(kayitDto));
		});

		app.MapPut("/kullanici/guncelle", async (IKullaniciService service, Kullanici guncelKullanici, HttpContext http) =>
		{
			var kullaniciIdStr = http.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;
			if (string.IsNullOrEmpty(kullaniciIdStr) || !Guid.TryParse(kullaniciIdStr, out var kullaniciId))
			{
				return Results.Unauthorized();
			}

			if (kullaniciId != guncelKullanici.Id)
			{
				return Results.Forbid();
			}

			var guncellenen = await service.Guncelle(kullaniciId, guncelKullanici);
			return guncellenen == null ? Results.NotFound() : Results.Ok(guncellenen);
		}).RequireAuthorization();




		app.MapPost("/kullanici/giris", async (IKullaniciService kullaniciService, KullaniciKayitDto kayitDto) =>
		{
			if (kayitDto == null || string.IsNullOrWhiteSpace(kayitDto.Eposta) || string.IsNullOrWhiteSpace(kayitDto.Sifre))
			{
				return Results.BadRequest(new { Message = "E-posta veya �ifre bo� olamaz." });
			}
			Kullanici kullanici = await kullaniciService.GirisYap(kayitDto);
			if (kullanici == null)
			{
				return Results.BadRequest(new { Message = "E-posta veya �ifre yanl��." });
			}

			var claims = new[]
			{
		new Claim(ClaimTypes.NameIdentifier, kullanici.Id.ToString()), // NameIdentifier kullan�ld�
        new Claim(ClaimTypes.Role, kullanici.Rol)
	};

			var jwtKey = builder.Configuration["Jwt:Key"];
			if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 16)
			{
				Console.WriteLine("Hata: Jwt:Key appsettings.json'da bo� veya �ok k�sa (en az 16 byte olmal�).");
				return Results.BadRequest(new { Message = "Sunucu yap�land�rma hatas�." });
			}

			var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
			var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				issuer: builder.Configuration["Jwt:Issuer"],
				audience: builder.Configuration["Jwt:Audience"],
				claims: claims,
				notBefore: DateTime.UtcNow,
				expires: DateTime.UtcNow.AddMinutes(builder.Configuration.GetValue("Jwt:ExpireMinutes", 60)),
				signingCredentials: credentials
			);

			var tokenHandler = new JwtSecurityTokenHandler();
			var tokenString = tokenHandler.WriteToken(token);

			if (string.IsNullOrEmpty(tokenString))
			{
				Console.WriteLine("Hata: Token olu�turulamad�. L�tfen Jwt yap�land�rmas�n� kontrol edin.");
				return Results.BadRequest(new { Message = "Token olu�turma hatas�." });
			}

			Console.WriteLine($"Olu�turulan Token: {tokenString}");
			return Results.Ok(new
			{
				Token = tokenString,
				Kullanici = new { kullanici.Id, kullanici.Eposta }
			});
		});


		app.MapPut("/isletme/guncelle/{id}", (Func<Guid, IIsletmeService, IsletmeGuncelleDto, Task<IResult>>)async delegate (Guid id, IIsletmeService service, IsletmeGuncelleDto guncelleDto)
		{
			try
			{
				Isletme guncel = await service.IsletmeGuncelle(id, guncelleDto);
				return (guncel == null) ? Results.NotFound((object)null) : Results.Ok<Isletme>(guncel);
			}
			catch (Exception ex2)
			{
				Exception ex = ex2;
				return Results.BadRequest(new
				{
					Message = "G�ncelleme ba�ar�s�z: " + ex.Message
				});
			}
		});
		app.MapDelete("/kullanici/sil/{id}", (Func<Guid, IKullaniciService, Task<IResult>>)(async (Guid id, IKullaniciService service) => (await service.Sil(id)) ? Results.Ok<string>("Hesap silindi.") : Results.NotFound<string>("Kullan�c� bulunamad�.")));


		app.MapPost("/kiralama/baslat", async (
			[FromBody] KiralamaBaslatDto dto,
			HttpContext context,
			IKiralamaService kiralamaService,
			IAracService aracService,
			KiralamaDbContext dbContext) =>
		{
			try
			{
				var kullaniciId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (string.IsNullOrEmpty(kullaniciId))
					return Results.Json(new { Message = "Yetkisiz eri�im." }, statusCode: StatusCodes.Status401Unauthorized);

				var yeniKiralama = new Kiralama
				{
					KullaniciId = Guid.Parse(kullaniciId),
					AracId = dto.AracId,
					BaslangicEnlem = dto.BaslangicEnlem,
					BaslangicBoylam = dto.BaslangicBoylam,
					BaslangicTarihi = DateTime.UtcNow,
					Durum = "Aktif",
					Ucret = 0m
				};

				// Kullan�c�n�n aktif kiralamas� var m� kontrol et
				var aktifKiralama = await kiralamaService.KullaniciKiralamaGecmisi(yeniKiralama.KullaniciId)
					.ContinueWith(t => t.Result.FirstOrDefault(k => k.Durum == "Aktif"));
				if (aktifKiralama != null)
					return Results.Json(new { Message = "Zaten aktif bir kiralaman�z var." }, statusCode: 400);

				using var transaction = await dbContext.Database.BeginTransactionAsync();
				try
				{
					var arac = await aracService.AracGetir(yeniKiralama.AracId);
					if (arac == null || !arac.MusaitMi)
						return Results.Json(new { Message = "Ara� uygun de�il." }, statusCode: 400);

					var kaydedilenKiralama = await kiralamaService.KiralamaBaslat(yeniKiralama);

					await aracService.AracGuncelle(arac.Id, new AracGuncelleDto
					{
						PlakaNumarasi = arac.PlakaNumarasi,
						Model = arac.Model,
						Tip = arac.Tip,
						SaatlikUcret = arac.SaatlikUcret,
						KonumEnlem = arac.KonumEnlem,
						KonumBoylam = arac.KonumBoylam,
						MusaitMi = false
					});

					await aracService.Ac(arac.Id);
					await transaction.CommitAsync();

					return Results.Ok(kaydedilenKiralama);
				}
				catch (Exception ex)
				{
					await transaction.RollbackAsync();
					return Results.Json(new { Message = $"Hata olu�tu: {ex.Message}" }, statusCode: 500);
				}
			}
			catch
			{
				return Results.Json(new { Message = "Beklenmeyen hata." }, statusCode: 500);
			}
		}).RequireAuthorization();



		app.MapGet("/kiralama/{id}", (Func<IKiralamaService, Guid, Task<IResult>>)async delegate (IKiralamaService service, Guid id)
		{
			Kiralama kiralama2 = await service.KiralamaGetir(id);
			return (kiralama2 == null) ? Results.NotFound((object)null) : Results.Ok<Kiralama>(kiralama2);
		});
		
		app.MapGet("/kiralama/kullanici/{kullaniciId}", (Func<IKiralamaService, Guid, Task<IResult>>)(async (IKiralamaService service, Guid kullaniciId) => Results.Ok<List<Kiralama>>(await service.KullaniciKiralamaGecmisi(kullaniciId))));


		app.MapPut("/kiralama/durum-guncelle/{id}", (Func<IKiralamaService, Guid, string, Task<IResult>>)(async (IKiralamaService service, Guid id, string durum) => (await service.KiralamaDurumGuncelle(id, durum)) ? Results.Ok<string>("Durum g�ncellendi.") : Results.NotFound<string>("Kiralama bulunamad�.")));
		
		
		app.MapGet("/kullanici/profil", async (HttpContext http, IKullaniciService service) =>
		{
			string kullaniciId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (string.IsNullOrEmpty(kullaniciId))
			{
				return Results.Unauthorized();
			}
			Guid kullaniciGuid = Guid.Parse(kullaniciId);
			Kullanici kullanici = await service.ProfilGetirAsync(kullaniciGuid);
			return (kullanici == null) ? Results.NotFound((object)null) : Results.Ok<Kullanici>(kullanici);
		}).RequireAuthorization();
		



		app.MapGet("/kullanici/profil/{kullaniciId}", (Func<Guid, IKullaniciService, Task<IResult>>)async delegate (Guid kullaniciId, IKullaniciService service)
		{
			Kullanici kullanici2 = await service.ProfilGetirAsync(kullaniciId);
			return (kullanici2 == null) ? Results.NotFound((object)null) : Results.Ok<Kullanici>(kullanici2);
		}).RequireAuthorization();


		// Program.cs i�inde, API endpoint tan�mlar�n�n oldu�u k�s�mda:

		app.MapPost("/araclar/nearby", async (LocationRequest request, IAracService aracService) =>
		{
			try
			{
				// MAUI uygulamas�ndan gelen veriyi konsola yazd�r�n
				Console.WriteLine($"API /araclar/nearby (POST) �a�r�ld�.");
				Console.WriteLine($"Gelen Enlem: {request.Enlem}, Boylam: {request.Boylam}, Mesafe: {request.MesafeKm}");

				if (request == null || request.Enlem == 0 || request.Boylam == 0 || request.MesafeKm <= 0)
				{
					return Results.BadRequest(new { Message = "Ge�ersiz konum veya mesafe de�eri." });
				}

				List<Arac> araclar = await aracService.GetNearbyAraclar(request.Enlem, request.Boylam, request.MesafeKm);

				if (araclar == null || araclar.Count == 0)
				{
					Console.WriteLine("Yak�nlarda ara� bulunamad�. Bo� liste d�n�l�yor.");
					return Results.Ok(new List<Arac>()); // Bo� bir liste d�nmek daha iyi bir kullan�c� deneyimi sa�lar
				}
				else
				{
					Console.WriteLine($"Toplam {araclar.Count} ara� bulundu. Ara�lar ba�ar�yla d�nd�r�ld�.");
					return Results.Ok(araclar);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Hata: {ex.Message}, StackTrace: {ex.StackTrace}");
				return Results.Json(new { Message = "Yak�ndaki ara�lar al�namad�: " + ex.Message }, statusCode: 500);
			}
		});


		app.MapPost("/admin/ekle", (Func<IKullaniciService, HttpContext, Task<IResult>>)async delegate (IKullaniciService kullaniciService, HttpContext httpContext)
		{
			string kullaniciId2 = httpContext.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
			if (string.IsNullOrEmpty(kullaniciId2))
			{
				return Results.Unauthorized();
			}
			Guid kullaniciGuid = Guid.Parse(kullaniciId2);
			Kullanici kullanici = await kullaniciService.ProfilGetirAsync(kullaniciGuid);
			if (kullanici == null)
			{
				return Results.NotFound<string>("Kullan�c� bulunamad�.");
			}
			if (kullanici.Rol != "Admin")
			{
				return Results.Forbid((AuthenticationProperties)null, (IList<string>)null);
			}
			kullanici.Rol = "Admin";
			return (await kullaniciService.KullaniciGuncelleAsync(kullanici)) ? Results.Ok<string>("Kullan�c� ba�ar�yla admin olarak g�ncellendi.") : Results.BadRequest<string>("Kullan�c� admin olarak g�ncellenemedi.");
		}).RequireAuthorization();


		app.MapGet("/isletme/arac/{id}", async (Guid id, IAracService aracService) =>
		{
			var arac = await aracService.AracGetir(id);
			return arac == null ? Results.NotFound(new { Message = "Ara� bulunamad�." }) : Results.Ok(arac);
		}).RequireAuthorization("IsletmeOnly");

		app.MapGet("/kiralama/{id}/kullanici", async (Guid id, IKiralamaService kiralamaService, IKullaniciService kullaniciService) =>
		{
			var kiralama = await kiralamaService.KiralamaGetir(id);
			if (kiralama == null || kiralama.KullaniciId == Guid.Empty) // KullaniciId null de�il, ama bo� Guid kontrol�
			{
				return Results.NotFound(new { Message = "Kiralama veya kullan�c� bilgisi bulunamad�." });
			}

			var kullanici = await kullaniciService.ProfilGetirAsync(kiralama.KullaniciId);
			if (kullanici == null)
			{
				return Results.NotFound(new { Message = "Kullan�c� bilgisi bulunamad�." });
			}

			return Results.Ok(new { kullanici.Id, kullanici.Ad, kullanici.Eposta });
		}).RequireAuthorization();


		app.MapPut("arac/{aracId}/bitir", async (Guid aracId, IAracService aracService) =>
		{
			try
			{
				var success = await aracService.KiralamaBitir(aracId);
				return success ? Results.Ok(new { Message = "Ara� ba�ar�yla m�sait hale getirildi." }) : Results.NotFound(new { Message = "Ara� bulunamad�." });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Hata: {ex.Message}\nStackTrace: {ex.StackTrace}");
				return Results.Json(new { Message = "Ara� durumu g�ncellenemedi: " + ex.Message }, statusCode: 500);
			}
		});

		app.MapPut("/kiralama/{kiralamaId}/bitir", async (Guid kiralamaId, IKiralamaService kiralamaService) =>
		{
			try
			{
				var success = await kiralamaService.KiralamaBitir(kiralamaId);
				return success ? Results.Ok(new { Message = "Kiralama ba�ar�yla pasif hale getirildi ve biti� tarihi eklendi." }) : Results.NotFound(new { Message = "Kiralama bulunamad�." });
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Hata: {ex.Message}\nStackTrace: {ex.StackTrace}");
				return Results.Json(new { Message = "Kiralama durumu g�ncellenemedi: " + ex.Message }, statusCode: 500);
			}
		});



		app.MapGet("/admin/kullanicilar", async (IKullaniciService kullaniciService) =>
		{
			var kullanicilar = await kullaniciService.TumKullanicilariGetir();
			return Results.Ok(kullanicilar);
		});

		app.MapGet("/admin/isletmeler", async (IIsletmeService isletmeService) =>
		{
			var isletmeler = await isletmeService.TumIsletmeleriGetir();
			return Results.Ok(isletmeler);
		});

		app.MapDelete("/admin/kullanici/sil/{id}", async (Guid id, IKullaniciService kullaniciService) =>
		{
			var basarili = await kullaniciService.Sil(id);
			return basarili ? Results.Ok(new { Message = "Kullan�c� silindi." }) : Results.NotFound(new { Message = "Kullan�c� bulunamad�." });
		}).RequireAuthorization(policy => policy.RequireRole("Admin"));

		app.MapDelete("/admin/isletme/sil/{id}", async (Guid id, IIsletmeService isletmeService) =>
		{
			var basarili = await isletmeService.IsletmeSil(id) != null;
			return basarili ? Results.Ok(new { Message = "��letme silindi." }) : Results.NotFound(new { Message = "��letme bulunamad�." });
		}).RequireAuthorization(policy => policy.RequireRole("Admin"));

		app.Run();
	}
}
