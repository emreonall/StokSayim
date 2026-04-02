using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StokSayim.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class mig5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErpKontrolOturumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SayimPlaniId = table.Column<int>(type: "int", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    TamamlanmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpKontrolOturumlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErpKontrolOturumlari_SayimPlanlari_SayimPlaniId",
                        column: x => x.SayimPlaniId,
                        principalTable: "SayimPlanlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ErpKontrolEkipler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ErpKontrolOturumuId = table.Column<int>(type: "int", nullable: false),
                    EkipId = table.Column<int>(type: "int", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    TamamlanmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpKontrolEkipler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErpKontrolEkipler_Ekipler_EkipId",
                        column: x => x.EkipId,
                        principalTable: "Ekipler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ErpKontrolEkipler_ErpKontrolOturumlari_ErpKontrolOturumuId",
                        column: x => x.ErpKontrolOturumuId,
                        principalTable: "ErpKontrolOturumlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ErpKontrolMalzemeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ErpKontrolEkipId = table.Column<int>(type: "int", nullable: false),
                    MalzemeKodu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MalzemeAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Birim = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SayilanMiktar = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Tamamlandi = table.Column<bool>(type: "bit", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpKontrolMalzemeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErpKontrolMalzemeler_ErpKontrolEkipler_ErpKontrolEkipId",
                        column: x => x.ErpKontrolEkipId,
                        principalTable: "ErpKontrolEkipler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErpKontrolEkipler_EkipId",
                table: "ErpKontrolEkipler",
                column: "EkipId");

            migrationBuilder.CreateIndex(
                name: "IX_ErpKontrolEkipler_ErpKontrolOturumuId",
                table: "ErpKontrolEkipler",
                column: "ErpKontrolOturumuId");

            migrationBuilder.CreateIndex(
                name: "IX_ErpKontrolMalzemeler_ErpKontrolEkipId",
                table: "ErpKontrolMalzemeler",
                column: "ErpKontrolEkipId");

            migrationBuilder.CreateIndex(
                name: "IX_ErpKontrolOturumlari_SayimPlaniId",
                table: "ErpKontrolOturumlari",
                column: "SayimPlaniId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErpKontrolMalzemeler");

            migrationBuilder.DropTable(
                name: "ErpKontrolEkipler");

            migrationBuilder.DropTable(
                name: "ErpKontrolOturumlari");
        }
    }
}
