using KiralamaAPI.Data;
using KiralamaAPI.Models;
using Microsoft.EntityFrameworkCore; 
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;


namespace KiralamaAPI.Service
{
	public class LoginResponse
	{
		public string Token { get; set; }
		public Isletme Isletme { get; set; }
	}
	public class IsletmeService : IIsletmeService
	{
		private readonly KiralamaDbContext _context;
		private readonly IConfiguration _configuration;

		public IsletmeService(KiralamaDbContext context, IConfiguration configuration)
		{
			_context = context;
			_configuration = configuration;
		}

		public async Task<List<Isletme>> TumIsletmeleriGetir()
		{
			return await _context.Isletmeler.AsNoTracking().ToListAsync();
		}

		public async Task<Isletme> IsletmeEkle(IsletmeKayitDto kayitDto)
		{
			var mevcutAd = await _context.Isletmeler.AnyAsync(i => i.Ad == kayitDto.Ad);
			if (mevcutAd)
				throw new InvalidOperationException("Bu işletme adı zaten kullanılıyor.");

			var mevcutEposta = await _context.Isletmeler.AnyAsync(i => i.Eposta == kayitDto.Eposta);
			if (mevcutEposta)
				throw new InvalidOperationException("Bu e-posta adresi zaten kullanılıyor.");

			var yeniIsletme = new Isletme
			{
				Id = Guid.NewGuid(),
				Ad = kayitDto.Ad,
				Eposta = kayitDto.Eposta,
				Adres = kayitDto.Adres ?? "", 
				KayitTarihi = DateTime.Now,
				Rol = "Isletme"
			};

			yeniIsletme.SifreHash = HashSifre(kayitDto.Sifre, yeniIsletme.Id);

			_context.Isletmeler.Add(yeniIsletme);
			await _context.SaveChangesAsync();
			return yeniIsletme;
		}

		public async Task<Isletme> IsletmeGuncelle(Guid id, IsletmeGuncelleDto guncelleDto)
		{
			Console.WriteLine($"Güncelleme denemesi: ID={id}, Eposta={guncelleDto.Eposta}");
			var mevcut = await _context.Isletmeler.FindAsync(id);
			if (mevcut == null)
			{
				Console.WriteLine("Isletme bulunamadı.");
				return null;
			}

			// Benzersizlik kontrolü
			if (!string.IsNullOrEmpty(guncelleDto.Ad) && mevcut.Ad != guncelleDto.Ad)
			{
				var adMevcut = await _context.Isletmeler
					.AnyAsync(i => i.Ad == guncelleDto.Ad && i.Id != id);
				if (adMevcut)
					return null;
			}

			if (!string.IsNullOrEmpty(guncelleDto.Eposta) && mevcut.Eposta != guncelleDto.Eposta)
			{
				var epostaMevcut = await _context.Isletmeler
					.AnyAsync(i => i.Eposta == guncelleDto.Eposta && i.Id != id);
				if (epostaMevcut)
					return null;
			}

			mevcut.Ad = guncelleDto.Ad ?? mevcut.Ad; // Null ise mevcut değeri koru
			mevcut.Adres = guncelleDto.Adres ?? mevcut.Adres; // Null ise mevcut değeri koru
			mevcut.Eposta = guncelleDto.Eposta ?? mevcut.Eposta; // Null ise mevcut değeri koru

			// Şifre güncelleniyorsa
			if (!string.IsNullOrWhiteSpace(guncelleDto.Sifre))
			{
				mevcut.SifreHash = HashSifre(guncelleDto.Sifre, mevcut.Id);
			}

			await _context.SaveChangesAsync();
			return mevcut;
		}

		public async Task<Isletme> IsletmeSil(Guid id)
		{
			var isletme = await _context.Isletmeler.FindAsync(id);
			if (isletme == null)
				return null;

			_context.Isletmeler.Remove(isletme);
			await _context.SaveChangesAsync();
			return isletme;
		}

		public async Task<Isletme> IsletmeGetir(Guid id)
		{
			return await _context.Isletmeler.FindAsync(id);
		}

		public async Task<LoginResponse> GirisYap(IsletmeGirisDto girisDto)
		{
			if (girisDto == null || string.IsNullOrWhiteSpace(girisDto.Eposta) || string.IsNullOrWhiteSpace(girisDto.Sifre))
			{
				Console.WriteLine("Giriş hatası: E-posta veya şifre boş.");
				return null;
			}

			var isletme = await _context.Isletmeler
				.AsNoTracking()
				.FirstOrDefaultAsync(i => i.Eposta == girisDto.Eposta);

			if (isletme == null || !SifreDogrula(girisDto.Sifre, isletme.SifreHash, isletme.Id))
			{
				Console.WriteLine($"Giriş başarısız: Eposta={girisDto.Eposta}, Şifre yanlış veya kullanıcı bulunamadı.");
				return null;
			}

			try
			{
				var token = GenerateJwtToken(isletme);
				if (string.IsNullOrEmpty(token))
				{
					Console.WriteLine("Hata: Token oluşturulamadı.");
					return null;
				}
				Console.WriteLine($"Token oluşturuldu: {token.Substring(0, 20)}..."); // İlk 20 karakteri log'la
				return new LoginResponse
				{
					Token = token,
					Isletme = isletme
				};
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Token oluşturma hatası: {ex.Message}");
				return null;
			}
		}

		private string GenerateJwtToken(Isletme isletme)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key", "JWT anahtarı appsettings.json'da tanımlı değil."));
			if (key.Length < 32)
			{
				Console.WriteLine("Uyarı: JWT anahtar uzunluğu 32 bayttan az. Güvenlik riski olabilir.");
			}

			var expireMinutes = _configuration.GetValue<int>("Jwt:ExpireMinutes", 60); // Varsayılan 60 dakika
			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(new[]
				{
			new Claim(ClaimTypes.NameIdentifier, isletme.Id.ToString()),
			new Claim(ClaimTypes.Email, isletme.Eposta),
			new Claim(ClaimTypes.Role, isletme.Rol)
		}),
				Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
				Issuer = _configuration["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer", "JWT issuer appsettings.json'da tanımlı değil."),
				Audience = _configuration["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience", "JWT audience appsettings.json'da tanımlı değil."),
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};

			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}

		public string HashSifre(string sifre, Guid isletmeId)
		{
			if (string.IsNullOrEmpty(sifre))
				throw new ArgumentNullException(nameof(sifre), "Şifre boş olamaz.");

			// Şifre ve isletmeId'yi birleştirip hash'le
			using var sha256 = SHA256.Create();
			var sifreVeTuz = Encoding.UTF8.GetBytes(sifre + isletmeId.ToString());
			var hash = sha256.ComputeHash(sifreVeTuz);
			return Convert.ToBase64String(hash);
		}

		public bool SifreDogrula(string sifre, string hash, Guid isletmeId)
		{
			if (string.IsNullOrEmpty(sifre) || string.IsNullOrEmpty(hash))
				return false;

			// Aynı isletmeId ile şifreyi tekrar hash'le
			var yeniHash = HashSifre(sifre, isletmeId);
			return yeniHash == hash;
		}


	}
}
