using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StokSayim.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class mig3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Deger3",
                table: "TurSonucuDetaylari",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deger3",
                table: "TurSonucuDetaylari");
        }
    }
}
