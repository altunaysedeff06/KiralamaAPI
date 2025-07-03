using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiralamaAPI.Migrations
{
    /// <inheritdoc />
    public partial class a : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Araclar_PlakaNumarasi",
                table: "Araclar");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Araclar",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "PlakaNumarasi",
                table: "Araclar",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_Araclar_PlakaNumarasi",
                table: "Araclar",
                column: "PlakaNumarasi",
                unique: true,
                filter: "[PlakaNumarasi] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Araclar_PlakaNumarasi",
                table: "Araclar");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Araclar",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PlakaNumarasi",
                table: "Araclar",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Araclar_PlakaNumarasi",
                table: "Araclar",
                column: "PlakaNumarasi",
                unique: true);
        }
    }
}
