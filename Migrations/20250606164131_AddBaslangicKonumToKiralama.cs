using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiralamaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddBaslangicKonumToKiralama : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bakiye",
                table: "Kullanicilar");

            migrationBuilder.AddColumn<double>(
                name: "BaslangicBoylam",
                table: "Kiralamalar",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BaslangicEnlem",
                table: "Kiralamalar",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaslangicBoylam",
                table: "Kiralamalar");

            migrationBuilder.DropColumn(
                name: "BaslangicEnlem",
                table: "Kiralamalar");

            migrationBuilder.AddColumn<decimal>(
                name: "Bakiye",
                table: "Kullanicilar",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
