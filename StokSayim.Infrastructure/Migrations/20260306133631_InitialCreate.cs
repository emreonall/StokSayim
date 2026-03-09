using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StokSayim.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AdSoyad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ekipler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EkipKodu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EkipAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ekipler", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SayimPlanlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanAdi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BaslangicTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    AktifEdilemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KapanisTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AktifEdenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SayimPlanlari", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EkipKullanicilari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EkipId = table.Column<int>(type: "int", nullable: false),
                    KullaniciId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BaslangicTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BitisTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AktifMi = table.Column<bool>(type: "bit", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EkipKullanicilari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EkipKullanicilari_AspNetUsers_KullaniciId",
                        column: x => x.KullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EkipKullanicilari_Ekipler_EkipId",
                        column: x => x.EkipId,
                        principalTable: "Ekipler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bolgeler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SayimPlaniId = table.Column<int>(type: "int", nullable: false),
                    BolgeKodu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BolgeAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Aciklama = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bolgeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bolgeler_SayimPlanlari_SayimPlaniId",
                        column: x => x.SayimPlaniId,
                        principalTable: "SayimPlanlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ErpStoklar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SayimPlaniId = table.Column<int>(type: "int", nullable: false),
                    MalzemeKodu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MalzemeAdi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DepoKodu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Miktar = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Birim = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LotNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SeriNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ImportTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportDosyaAdi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErpStoklar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErpStoklar_SayimPlanlari_SayimPlaniId",
                        column: x => x.SayimPlaniId,
                        principalTable: "SayimPlanlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SayimPlanDepoKodlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SayimPlaniId = table.Column<int>(type: "int", nullable: false),
                    DepoKodu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepoAdi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SayimPlanDepoKodlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SayimPlanDepoKodlari_SayimPlanlari_SayimPlaniId",
                        column: x => x.SayimPlaniId,
                        principalTable: "SayimPlanlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EkipGruplari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BolgeId = table.Column<int>(type: "int", nullable: false),
                    SayimPlaniId = table.Column<int>(type: "int", nullable: false),
                    EkipGrubuAdi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EkipGruplari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EkipGruplari_Bolgeler_BolgeId",
                        column: x => x.BolgeId,
                        principalTable: "Bolgeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EkipGruplari_SayimPlanlari_SayimPlaniId",
                        column: x => x.SayimPlaniId,
                        principalTable: "SayimPlanlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SayimOturumlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BolgeId = table.Column<int>(type: "int", nullable: false),
                    SayimPlaniId = table.Column<int>(type: "int", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    AktifTurNo = table.Column<int>(type: "int", nullable: false),
                    BaslangicTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KapanisTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SorumluKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SayimOturumlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SayimOturumlari_Bolgeler_BolgeId",
                        column: x => x.BolgeId,
                        principalTable: "Bolgeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SayimOturumlari_SayimPlanlari_SayimPlaniId",
                        column: x => x.SayimPlaniId,
                        principalTable: "SayimPlanlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EkipGrubuEkipler",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EkipGrubuId = table.Column<int>(type: "int", nullable: false),
                    EkipId = table.Column<int>(type: "int", nullable: false),
                    SiraNo = table.Column<int>(type: "int", nullable: false),
                    EkipRolu = table.Column<int>(type: "int", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EkipGrubuEkipler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EkipGrubuEkipler_EkipGruplari_EkipGrubuId",
                        column: x => x.EkipGrubuId,
                        principalTable: "EkipGruplari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EkipGrubuEkipler_Ekipler_EkipId",
                        column: x => x.EkipId,
                        principalTable: "Ekipler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SayimTurleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SayimOturumuId = table.Column<int>(type: "int", nullable: false),
                    TurNo = table.Column<int>(type: "int", nullable: false),
                    TurTipi = table.Column<int>(type: "int", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    AcilamaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KapanmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notlar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SayimTurleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SayimTurleri_SayimOturumlari_SayimOturumuId",
                        column: x => x.SayimOturumuId,
                        principalTable: "SayimOturumlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GorevBildirimleri",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SayimOturumuId = table.Column<int>(type: "int", nullable: false),
                    SayimTuruId = table.Column<int>(type: "int", nullable: true),
                    BildirimTipi = table.Column<int>(type: "int", nullable: false),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    Mesaj = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IslemTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsleyenKullaniciId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GorevBildirimleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GorevBildirimleri_AspNetUsers_IsleyenKullaniciId",
                        column: x => x.IsleyenKullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GorevBildirimleri_SayimOturumlari_SayimOturumuId",
                        column: x => x.SayimOturumuId,
                        principalTable: "SayimOturumlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GorevBildirimleri_SayimTurleri_SayimTuruId",
                        column: x => x.SayimTuruId,
                        principalTable: "SayimTurleri",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SayimKayitlari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SayimTuruId = table.Column<int>(type: "int", nullable: false),
                    EkipId = table.Column<int>(type: "int", nullable: false),
                    EkipRolu = table.Column<int>(type: "int", nullable: false),
                    SayimYapanKullaniciId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BaslangicTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TamamlanmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    Notlar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SayimKayitlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SayimKayitlari_AspNetUsers_SayimYapanKullaniciId",
                        column: x => x.SayimYapanKullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SayimKayitlari_Ekipler_EkipId",
                        column: x => x.EkipId,
                        principalTable: "Ekipler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SayimKayitlari_SayimTurleri_SayimTuruId",
                        column: x => x.SayimTuruId,
                        principalTable: "SayimTurleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TurSonuclari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SayimTuruId = table.Column<int>(type: "int", nullable: false),
                    ToplamMalzemeSayisi = table.Column<int>(type: "int", nullable: false),
                    EslesilenSayisi = table.Column<int>(type: "int", nullable: false),
                    FarkliSayisi = table.Column<int>(type: "int", nullable: false),
                    GenelDurum = table.Column<int>(type: "int", nullable: false),
                    HesaplamaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurSonuclari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TurSonuclari_SayimTurleri_SayimTuruId",
                        column: x => x.SayimTuruId,
                        principalTable: "SayimTurleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SayimKaydiDetaylari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SayimKaydiId = table.Column<int>(type: "int", nullable: false),
                    MalzemeKodu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MalzemeAdi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LotNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SeriNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SayilanMiktar = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Birim = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Notlar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SayimKaydiDetaylari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SayimKaydiDetaylari_SayimKayitlari_SayimKaydiId",
                        column: x => x.SayimKaydiId,
                        principalTable: "SayimKayitlari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SayimTuruKatilimcilari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SayimTuruId = table.Column<int>(type: "int", nullable: false),
                    EkipId = table.Column<int>(type: "int", nullable: false),
                    EkipRolu = table.Column<int>(type: "int", nullable: false),
                    SayimKaydiId = table.Column<int>(type: "int", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SayimTuruKatilimcilari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SayimTuruKatilimcilari_Ekipler_EkipId",
                        column: x => x.EkipId,
                        principalTable: "Ekipler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SayimTuruKatilimcilari_SayimKayitlari_SayimKaydiId",
                        column: x => x.SayimKaydiId,
                        principalTable: "SayimKayitlari",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SayimTuruKatilimcilari_SayimTurleri_SayimTuruId",
                        column: x => x.SayimTuruId,
                        principalTable: "SayimTurleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TurSonucuDetaylari",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TurSonucuId = table.Column<int>(type: "int", nullable: false),
                    MalzemeKodu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MalzemeAdi = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LotNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SeriNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Birim = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Deger1 = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Deger2 = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Fark = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    FarkYuzdesi = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Durum = table.Column<int>(type: "int", nullable: false),
                    OnaylananDeger = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    KararTipi = table.Column<int>(type: "int", nullable: true),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurSonucuDetaylari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TurSonucuDetaylari_TurSonuclari_TurSonucuId",
                        column: x => x.TurSonucuId,
                        principalTable: "TurSonuclari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManuelKararlar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TurSonucuDetayId = table.Column<int>(type: "int", nullable: false),
                    SayimTuruId = table.Column<int>(type: "int", nullable: false),
                    MalzemeKodu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LotNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KararVerilenDeger = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Gerekce = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KararVerenKullaniciId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    KararTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OlusturmaTarihi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuncellemeTarihi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OlusturanKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GuncelleyenKullaniciId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManuelKararlar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManuelKararlar_AspNetUsers_KararVerenKullaniciId",
                        column: x => x.KararVerenKullaniciId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ManuelKararlar_SayimTurleri_SayimTuruId",
                        column: x => x.SayimTuruId,
                        principalTable: "SayimTurleri",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ManuelKararlar_TurSonucuDetaylari_TurSonucuDetayId",
                        column: x => x.TurSonucuDetayId,
                        principalTable: "TurSonucuDetaylari",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Bolgeler_SayimPlaniId_BolgeKodu",
                table: "Bolgeler",
                columns: new[] { "SayimPlaniId", "BolgeKodu" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EkipGrubuEkipler_EkipGrubuId",
                table: "EkipGrubuEkipler",
                column: "EkipGrubuId");

            migrationBuilder.CreateIndex(
                name: "IX_EkipGrubuEkipler_EkipId",
                table: "EkipGrubuEkipler",
                column: "EkipId");

            migrationBuilder.CreateIndex(
                name: "IX_EkipGruplari_BolgeId",
                table: "EkipGruplari",
                column: "BolgeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EkipGruplari_SayimPlaniId",
                table: "EkipGruplari",
                column: "SayimPlaniId");

            migrationBuilder.CreateIndex(
                name: "IX_EkipKullanicilari_EkipId",
                table: "EkipKullanicilari",
                column: "EkipId");

            migrationBuilder.CreateIndex(
                name: "IX_EkipKullanicilari_KullaniciId",
                table: "EkipKullanicilari",
                column: "KullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_Ekipler_EkipKodu",
                table: "Ekipler",
                column: "EkipKodu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ErpStoklar_SayimPlaniId_MalzemeKodu_DepoKodu_LotNo",
                table: "ErpStoklar",
                columns: new[] { "SayimPlaniId", "MalzemeKodu", "DepoKodu", "LotNo" });

            migrationBuilder.CreateIndex(
                name: "IX_GorevBildirimleri_IsleyenKullaniciId",
                table: "GorevBildirimleri",
                column: "IsleyenKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_GorevBildirimleri_SayimOturumuId",
                table: "GorevBildirimleri",
                column: "SayimOturumuId");

            migrationBuilder.CreateIndex(
                name: "IX_GorevBildirimleri_SayimTuruId",
                table: "GorevBildirimleri",
                column: "SayimTuruId");

            migrationBuilder.CreateIndex(
                name: "IX_ManuelKararlar_KararVerenKullaniciId",
                table: "ManuelKararlar",
                column: "KararVerenKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_ManuelKararlar_SayimTuruId",
                table: "ManuelKararlar",
                column: "SayimTuruId");

            migrationBuilder.CreateIndex(
                name: "IX_ManuelKararlar_TurSonucuDetayId",
                table: "ManuelKararlar",
                column: "TurSonucuDetayId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SayimKaydiDetaylari_SayimKaydiId",
                table: "SayimKaydiDetaylari",
                column: "SayimKaydiId");

            migrationBuilder.CreateIndex(
                name: "IX_SayimKayitlari_EkipId",
                table: "SayimKayitlari",
                column: "EkipId");

            migrationBuilder.CreateIndex(
                name: "IX_SayimKayitlari_SayimTuruId",
                table: "SayimKayitlari",
                column: "SayimTuruId");

            migrationBuilder.CreateIndex(
                name: "IX_SayimKayitlari_SayimYapanKullaniciId",
                table: "SayimKayitlari",
                column: "SayimYapanKullaniciId");

            migrationBuilder.CreateIndex(
                name: "IX_SayimOturumlari_BolgeId",
                table: "SayimOturumlari",
                column: "BolgeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SayimOturumlari_SayimPlaniId",
                table: "SayimOturumlari",
                column: "SayimPlaniId");

            migrationBuilder.CreateIndex(
                name: "IX_SayimPlanDepoKodlari_SayimPlaniId",
                table: "SayimPlanDepoKodlari",
                column: "SayimPlaniId");

            migrationBuilder.CreateIndex(
                name: "IX_SayimTurleri_SayimOturumuId",
                table: "SayimTurleri",
                column: "SayimOturumuId");

            migrationBuilder.CreateIndex(
                name: "IX_SayimTuruKatilimcilari_EkipId",
                table: "SayimTuruKatilimcilari",
                column: "EkipId");

            migrationBuilder.CreateIndex(
                name: "IX_SayimTuruKatilimcilari_SayimKaydiId",
                table: "SayimTuruKatilimcilari",
                column: "SayimKaydiId");

            migrationBuilder.CreateIndex(
                name: "IX_SayimTuruKatilimcilari_SayimTuruId",
                table: "SayimTuruKatilimcilari",
                column: "SayimTuruId");

            migrationBuilder.CreateIndex(
                name: "IX_TurSonuclari_SayimTuruId",
                table: "TurSonuclari",
                column: "SayimTuruId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TurSonucuDetaylari_TurSonucuId",
                table: "TurSonucuDetaylari",
                column: "TurSonucuId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "EkipGrubuEkipler");

            migrationBuilder.DropTable(
                name: "EkipKullanicilari");

            migrationBuilder.DropTable(
                name: "ErpStoklar");

            migrationBuilder.DropTable(
                name: "GorevBildirimleri");

            migrationBuilder.DropTable(
                name: "ManuelKararlar");

            migrationBuilder.DropTable(
                name: "SayimKaydiDetaylari");

            migrationBuilder.DropTable(
                name: "SayimPlanDepoKodlari");

            migrationBuilder.DropTable(
                name: "SayimTuruKatilimcilari");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "EkipGruplari");

            migrationBuilder.DropTable(
                name: "TurSonucuDetaylari");

            migrationBuilder.DropTable(
                name: "SayimKayitlari");

            migrationBuilder.DropTable(
                name: "TurSonuclari");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Ekipler");

            migrationBuilder.DropTable(
                name: "SayimTurleri");

            migrationBuilder.DropTable(
                name: "SayimOturumlari");

            migrationBuilder.DropTable(
                name: "Bolgeler");

            migrationBuilder.DropTable(
                name: "SayimPlanlari");
        }
    }
}
