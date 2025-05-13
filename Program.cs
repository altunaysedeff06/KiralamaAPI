using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using KiralamaAPI.Data;
using KiralamaAPI.Models;
using KiralamaAPI.Service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace KiralamaAPI;

public class Program
{
	public static void Main(string[] args)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
		string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
		builder.Services.AddDbContext<KiralamaDbContext>(delegate (DbContextOptionsBuilder options)
		{
			SqlServerDbContextOptionsExtensions.UseSqlServer(options, connectionString, (Action<SqlServerDbContextOptionsBuilder>)null);
		});
		builder.Services.AddAuthentication("Bearer").AddJwtBearer(delegate (JwtBearerOptions options)
		{
			
			options.TokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidateLifetime = false,
				ValidateIssuerSigningKey = true,
				ValidIssuer = builder.Configuration["Jwt:Issuer"],
				ValidAudience = builder.Configuration["Jwt:Audience"],
				IssuerSigningKey = (SecurityKey)new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
			};
		});
		builder.Services.AddScoped<IAracService, AracService>();
		builder.Services.AddScoped<IIsletmeService, IsletmeService>();
		builder.Services.AddScoped<IKullaniciService, KullaniciService>();
		builder.Services.AddScoped<IKiralamaService, KiralamaService>();
		builder.Services.AddScoped<IBildirimService, BildirimService>();
		builder.Services.AddControllers();
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
					 policy.RequireClaim(ClaimTypes.Role, "Isletme"));
		});
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen(delegate (SwaggerGenOptions c)
		{
			c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
			{
				In = ParameterLocation.Header,
				Description = "Please enter JWT with Bearer into field",
				Name = "Authorization",
				Type = SecuritySchemeType.ApiKey
			});
			c.AddSecurityRequirement(new OpenApiSecurityRequirement {
			{
				new OpenApiSecurityScheme
				{
					Reference = new OpenApiReference
					{
						Type = ReferenceType.SecurityScheme,
						Id = "Bearer"
					}
				},
				new string[0]
			} });
		});
		WebApplication app = builder.Build();
		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
		}
		app.UseAuthentication();
		app.UseAuthorization();
		app.MapPost("/isletme/arac/ekle", (Func<IAracService, Arac, Task<IResult>>)(async (IAracService aracService, Arac arac) => Results.Ok<Arac>(await aracService.AracEkle(arac))));
		app.MapPut("/isletme/arac/guncelle/{id}", (Func<int, IAracService, Arac, Task<IResult>>)async delegate (int id, IAracService aracService, Arac arac)
		{
			Arac guncellenenArac = await aracService.AracGuncelle(id, arac);
			return (guncellenenArac == null) ? Results.NotFound<string>("Araç bulunamadý.") : Results.Ok<Arac>(guncellenenArac);
		});
		app.MapDelete("/isletme/arac/sil/{id}", (Func<int, IAracService, Task<IResult>>)(async (int id, IAracService aracService) => (await aracService.AracSil(id) == null) ? Results.NotFound<string>("Araç bulunamadý.") : Results.Ok<string>("Araç baþarýyla silindi.")));
		app.MapGet("/isletme/araclar", (Func<IAracService, Task<IResult>>)(async (IAracService aracService) => Results.Ok<List<Arac>>(await aracService.AraclariListele())));
		app.MapPost("/isletme/kayit", (Func<IIsletmeService, IsletmeKayitDto, Task<IResult>>)async delegate (IIsletmeService isletmeService, IsletmeKayitDto kayitDto)
		{
			try
			{
				if (kayitDto == null || string.IsNullOrWhiteSpace(kayitDto.Ad) || string.IsNullOrWhiteSpace(kayitDto.Eposta) || string.IsNullOrWhiteSpace(kayitDto.Sifre))
				{
					return Results.BadRequest(new
					{
						Message = "Ýþletme adý, e-posta veya þifre boþ olamaz."
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
					Message = "Veritabaný hatasý: " + ex6.InnerException?.Message
				});
			}
		});
		app.MapPost("/isletme/giris", async (
	IIsletmeService isletmeService,
	IConfiguration configuration,
	IsletmeGirisDto girisDto) =>
		{
			var isletme = await isletmeService.GirisYap(girisDto);
			if (isletme == null)
			{
				return Results.BadRequest(new { Message = "E-posta veya þifre yanlýþ." });
			}

			var claims = new[]
			{
		new Claim(ClaimTypes.Name, isletme.Id.ToString()),
		new Claim(ClaimTypes.Role, "Isletme")
	};

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
			var expires = DateTime.Now.AddMinutes(configuration.GetValue<int>("Jwt:ExpireMinutes", 60));

			var token = new JwtSecurityToken(
				issuer: configuration["Jwt:Issuer"],
				audience: configuration["Jwt:Audience"],
				claims: claims,
				expires: expires,
				signingCredentials: creds
			);

			var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

			return Results.Ok(new { Token = tokenString });
		});



		app.MapDelete("/isletme/sil/{id}",	async (Guid id, IIsletmeService service) =>	(await service.IsletmeSil(id) == null)
			? Results.NotFound((object)null)
			: Results.Ok<string>("Ýþletme silindi"));

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
		app.MapPost("/kullanici/giris", (Func<IKullaniciService, KullaniciKayitDto, Task<IResult>>)async delegate (IKullaniciService kullaniciService, KullaniciKayitDto kayitDto)
		{
			if (kayitDto == null || string.IsNullOrWhiteSpace(kayitDto.Eposta) || string.IsNullOrWhiteSpace(kayitDto.Sifre))
			{
				return Results.BadRequest(new
				{
					Message = "E-posta veya þifre boþ olamaz."
				});
			}
			Kullanici kullanici4 = await kullaniciService.GirisYap(kayitDto);
			if (kullanici4 == null)
			{
				return Results.BadRequest(new
				{
					Message = "E-posta veya þifre yanlýþ."
				});
			}
			Claim[] claims = new Claim[2]
			{
				new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", kullanici4.Id.ToString()),
				new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", kullanici4.Rol)
			};
			string jwtKey = builder.Configuration["Jwt:Key"];
			SymmetricSecurityKey securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
			SigningCredentials credentials = new SigningCredentials((SecurityKey)(object)securityKey, "HS256");
			string? text = builder.Configuration["Jwt:Issuer"];
			string? text2 = builder.Configuration["Jwt:Audience"];
			DateTime? dateTime = DateTime.Now.AddMinutes(builder.Configuration.GetValue("Jwt:ExpireMinutes", 60));
			JwtSecurityToken token = new JwtSecurityToken(text, text2, (IEnumerable<Claim>)claims, (DateTime?)null, dateTime, credentials);
			string tokenString = ((SecurityTokenHandler)new JwtSecurityTokenHandler()).WriteToken((SecurityToken)(object)token);
			return Results.Ok(new
			{
				Token = tokenString
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
					Message = "Güncelleme baþarýsýz: " + ex.Message
				});
			}
		});
		app.MapDelete("/kullanici/sil/{id}", (Func<Guid, IKullaniciService, Task<IResult>>)(async (Guid id, IKullaniciService service) => (await service.Sil(id)) ? Results.Ok<string>("Hesap silindi.") : Results.NotFound<string>("Kullanýcý bulunamadý.")));
		app.MapPost("/kiralama/baslat", (Func<IKiralamaService, Kiralama, Task<IResult>>)(async (IKiralamaService service, Kiralama kiralama) => Results.Ok<Kiralama>(await service.KiralamaBaslat(kiralama))));
		app.MapGet("/kiralama/{id}", (Func<IKiralamaService, Guid, Task<IResult>>)async delegate (IKiralamaService service, Guid id)
		{
			Kiralama kiralama2 = await service.KiralamaGetir(id);
			return (kiralama2 == null) ? Results.NotFound((object)null) : Results.Ok<Kiralama>(kiralama2);
		});
		app.MapGet("/kiralama/kullanici/{kullaniciId}", (Func<IKiralamaService, Guid, Task<IResult>>)(async (IKiralamaService service, Guid kullaniciId) => Results.Ok<List<Kiralama>>(await service.KullaniciKiralamaGecmisi(kullaniciId))));
		app.MapPut("/kiralama/durum-guncelle/{id}", (Func<IKiralamaService, Guid, string, Task<IResult>>)(async (IKiralamaService service, Guid id, string durum) => (await service.KiralamaDurumGuncelle(id, durum)) ? Results.Ok<string>("Durum güncellendi.") : Results.NotFound<string>("Kiralama bulunamadý.")));
		app.MapGet("/kullanici/profil", (Func<HttpContext, IKullaniciService, Task<IResult>>)async delegate (HttpContext http, IKullaniciService service)
		{
			string kullaniciId3 = http.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;
			if (string.IsNullOrEmpty(kullaniciId3))
			{
				return Results.Unauthorized();
			}
			Guid kullaniciGuid2 = Guid.Parse(kullaniciId3);
			Kullanici kullanici3 = await service.ProfilGetirAsync(kullaniciGuid2);
			return (kullanici3 == null) ? Results.NotFound((object)null) : Results.Ok<Kullanici>(kullanici3);
		});
		app.MapGet("/kullanici/profil/{kullaniciId}", (Func<Guid, IKullaniciService, Task<IResult>>)async delegate (Guid kullaniciId, IKullaniciService service)
		{
			Kullanici kullanici2 = await service.ProfilGetirAsync(kullaniciId);
			return (kullanici2 == null) ? Results.NotFound((object)null) : Results.Ok<Kullanici>(kullanici2);
		}).RequireAuthorization();
		app.MapPost("/araclar/nearby", (Func<LocationRequest, IAracService, Task<IResult>>)async delegate (LocationRequest request, IAracService aracService)
		{
			List<Arac> araclar = await aracService.GetNearbyAraclar(request.Enlem, request.Boylam, request.MesafeKm);
			return (araclar == null || araclar.Count == 0) ? Results.NotFound<string>("Yakýnlarda araç bulunamadý.") : Results.Ok<List<Arac>>(araclar);
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
				return Results.NotFound<string>("Kullanýcý bulunamadý.");
			}
			if (kullanici.Rol != "Admin")
			{
				return Results.Forbid((AuthenticationProperties)null, (IList<string>)null);
			}
			kullanici.Rol = "Admin";
			return (await kullaniciService.KullaniciGuncelleAsync(kullanici)) ? Results.Ok<string>("Kullanýcý baþarýyla admin olarak güncellendi.") : Results.BadRequest<string>("Kullanýcý admin olarak güncellenemedi.");
		}).RequireAuthorization();
		app.MapGet("/arac/{id}/kullanici", (Func<Guid, IAracService, Task<IResult>>)async delegate (Guid id, IAracService aracService)
		{
			Arac arac3 = await aracService.AracGetir(id);
			if (arac3 == null)
			{
				return Results.NotFound<string>("Araç ve kullanýcý bilgisi bulunamadý.");
			}
			var aracKullanici = new
			{
				arac3.Model,
				arac3.User.Id,
				arac3.User.Ad,
				arac3.User.Eposta
			};
			return Results.Ok(aracKullanici);
		});
		app.MapPost("/arac/{id}/kilitle", (Func<Guid, IAracService, IBildirimService, Task<IResult>>)async delegate (Guid id, IAracService aracService, IBildirimService bildirimService)
		{
			Arac arac2 = await aracService.AracGetir(id);
			if (arac2 == null)
			{
				return Results.NotFound<string>("Araç bulunamadý.");
			}
			if (await aracService.Kilitle(id))
			{
				await bildirimService.SendNotificationAsync(arac2.UserId, "Araç baþarýyla kilitlendi.");
				return Results.Ok<string>("Araç baþarýyla kilitlendi.");
			}
			return Results.BadRequest<string>("Kilitleme iþlemi baþarýsýz.");
		});
		app.Run();
	}
}
