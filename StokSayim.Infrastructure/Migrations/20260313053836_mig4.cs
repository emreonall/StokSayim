using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StokSayim.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class mig4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Birim",
                table: "TurSonucuDetaylari");

            migrationBuilder.DropColumn(
                name: "MalzemeAdi",
                table: "TurSonucuDetaylari");

            migrationBuilder.DropColumn(
                name: "Birim",
                table: "SayimKaydiDetaylari");

            migrationBuilder.DropColumn(
                name: "MalzemeAdi",
                table: "SayimKaydiDetaylari");

            migrationBuilder.DropColumn(
                name: "Birim",
                table: "ErpStoklar");

            migrationBuilder.DropColumn(
                name: "MalzemeAdi",
                table: "ErpStoklar");

            migrationBuilder.AlterColumn<string>(
                name: "MalzemeKodu",
                table: "TurSonucuDetaylari",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Deger3",
                table: "TurSonucuDetaylari",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Malzemeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MalzemeKodu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MalzemeAdi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OlcuBirimi = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    SonGuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeKaynagi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Malzemeler", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Malzemeler_MalzemeKodu",
                table: "Malzemeler",
                column: "MalzemeKodu",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Malzemeler");

            migrationBuilder.AlterColumn<string>(
                name: "MalzemeKodu",
                table: "TurSonucuDetaylari",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<decimal>(
                name: "Deger3",
                table: "TurSonucuDetaylari",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)",
                oldPrecision: 18,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Birim",
                table: "TurSonucuDetaylari",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MalzemeAdi",
                table: "TurSonucuDetaylari",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Birim",
                table: "SayimKaydiDetaylari",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MalzemeAdi",
                table: "SayimKaydiDetaylari",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Birim",
                table: "ErpStoklar",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MalzemeAdi",
                table: "ErpStoklar",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }
    }
}
