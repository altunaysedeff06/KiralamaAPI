using KiralamaAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace KiralamaAPI.Data
{
	public class KiralamaDbContext : DbContext
	{
		public KiralamaDbContext(DbContextOptions<KiralamaDbContext> options) : base(options)
		{
		}

		public DbSet<Kullanici> Kullanicilar { get; set; }
		public DbSet<Isletme> Isletmeler { get; set; }
		public DbSet<Arac> Araclar { get; set; }
		public DbSet<Kiralama> Kiralamalar { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Kullanıcı e-posta alanının benzersiz olması için
			modelBuilder.Entity<Kullanici>()
				.HasIndex(k => k.Eposta)
				.IsUnique();

			// İşletme e-posta alanının benzersiz olması için
			modelBuilder.Entity<Isletme>()
				.HasIndex(i => i.Eposta)
				.IsUnique();

			// Araçların plaka numarasının benzersiz olması için
			modelBuilder.Entity<Arac>()
				.HasIndex(a => a.PlakaNumarasi)
				.IsUnique();

			// Kullanıcı - Kiralama ilişkisi (1 Kullanıcı, birden fazla kiralama yapabilir)
			modelBuilder.Entity<Kiralama>()
				.HasOne(k => k.Kullanici)
				.WithMany()
				.HasForeignKey(k => k.KullaniciId)
				.OnDelete(DeleteBehavior.Cascade);

			// Araç - Kiralama ilişkisi (1 Araç, birden fazla kiralama kaydına sahip olabilir)
			modelBuilder.Entity<Kiralama>()
				.HasOne(k => k.Arac)
				.WithMany()
				.HasForeignKey(k => k.AracId)
				.OnDelete(DeleteBehavior.Cascade);

			// İşletme - Araç ilişkisi (1 İşletme, birden fazla araca sahip olabilir)
			modelBuilder.Entity<Arac>()
				.HasOne(a => a.Isletme)
				.WithMany()
				.HasForeignKey(a => a.IsletmeId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}
