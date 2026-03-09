using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StokSayim.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class mig2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GorevBildirimleri_SayimTurleri_SayimTuruId",
                table: "GorevBildirimleri");

            migrationBuilder.DropForeignKey(
                name: "FK_ManuelKararlar_SayimTurleri_SayimTuruId",
                table: "ManuelKararlar");

            migrationBuilder.DropForeignKey(
                name: "FK_SayimKayitlari_SayimTurleri_SayimTuruId",
                table: "SayimKayitlari");

            migrationBuilder.DropForeignKey(
                name: "FK_SayimTurleri_SayimOturumlari_SayimOturumuId",
                table: "SayimTurleri");

            migrationBuilder.DropForeignKey(
                name: "FK_SayimTuruKatilimcilari_SayimTurleri_SayimTuruId",
                table: "SayimTuruKatilimcilari");

            migrationBuilder.DropForeignKey(
                name: "FK_TurSonuclari_SayimTurleri_SayimTuruId",
                table: "TurSonuclari");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SayimTurleri",
                table: "SayimTurleri");

            migrationBuilder.RenameTable(
                name: "SayimTurleri",
                newName: "SayimTurlari");

            migrationBuilder.RenameIndex(
                name: "IX_SayimTurleri_SayimOturumuId",
                table: "SayimTurlari",
                newName: "IX_SayimTurlari_SayimOturumuId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SayimTurlari",
                table: "SayimTurlari",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GorevBildirimleri_SayimTurlari_SayimTuruId",
                table: "GorevBildirimleri",
                column: "SayimTuruId",
                principalTable: "SayimTurlari",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ManuelKararlar_SayimTurlari_SayimTuruId",
                table: "ManuelKararlar",
                column: "SayimTuruId",
                principalTable: "SayimTurlari",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SayimKayitlari_SayimTurlari_SayimTuruId",
                table: "SayimKayitlari",
                column: "SayimTuruId",
                principalTable: "SayimTurlari",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SayimTurlari_SayimOturumlari_SayimOturumuId",
                table: "SayimTurlari",
                column: "SayimOturumuId",
                principalTable: "SayimOturumlari",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SayimTuruKatilimcilari_SayimTurlari_SayimTuruId",
                table: "SayimTuruKatilimcilari",
                column: "SayimTuruId",
                principalTable: "SayimTurlari",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TurSonuclari_SayimTurlari_SayimTuruId",
                table: "TurSonuclari",
                column: "SayimTuruId",
                principalTable: "SayimTurlari",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GorevBildirimleri_SayimTurlari_SayimTuruId",
                table: "GorevBildirimleri");

            migrationBuilder.DropForeignKey(
                name: "FK_ManuelKararlar_SayimTurlari_SayimTuruId",
                table: "ManuelKararlar");

            migrationBuilder.DropForeignKey(
                name: "FK_SayimKayitlari_SayimTurlari_SayimTuruId",
                table: "SayimKayitlari");

            migrationBuilder.DropForeignKey(
                name: "FK_SayimTurlari_SayimOturumlari_SayimOturumuId",
                table: "SayimTurlari");

            migrationBuilder.DropForeignKey(
                name: "FK_SayimTuruKatilimcilari_SayimTurlari_SayimTuruId",
                table: "SayimTuruKatilimcilari");

            migrationBuilder.DropForeignKey(
                name: "FK_TurSonuclari_SayimTurlari_SayimTuruId",
                table: "TurSonuclari");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SayimTurlari",
                table: "SayimTurlari");

            migrationBuilder.RenameTable(
                name: "SayimTurlari",
                newName: "SayimTurleri");

            migrationBuilder.RenameIndex(
                name: "IX_SayimTurlari_SayimOturumuId",
                table: "SayimTurleri",
                newName: "IX_SayimTurleri_SayimOturumuId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SayimTurleri",
                table: "SayimTurleri",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GorevBildirimleri_SayimTurleri_SayimTuruId",
                table: "GorevBildirimleri",
                column: "SayimTuruId",
                principalTable: "SayimTurleri",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ManuelKararlar_SayimTurleri_SayimTuruId",
                table: "ManuelKararlar",
                column: "SayimTuruId",
                principalTable: "SayimTurleri",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SayimKayitlari_SayimTurleri_SayimTuruId",
                table: "SayimKayitlari",
                column: "SayimTuruId",
                principalTable: "SayimTurleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SayimTurleri_SayimOturumlari_SayimOturumuId",
                table: "SayimTurleri",
                column: "SayimOturumuId",
                principalTable: "SayimOturumlari",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SayimTuruKatilimcilari_SayimTurleri_SayimTuruId",
                table: "SayimTuruKatilimcilari",
                column: "SayimTuruId",
                principalTable: "SayimTurleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TurSonuclari_SayimTurleri_SayimTuruId",
                table: "TurSonuclari",
                column: "SayimTuruId",
                principalTable: "SayimTurleri",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
