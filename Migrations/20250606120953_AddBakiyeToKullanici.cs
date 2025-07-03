using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiralamaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddBakiyeToKullanici : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Bakiye",
                table: "Kullanicilar",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bakiye",
                table: "Kullanicilar");
        }
    }
}
