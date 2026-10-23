using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNS.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionAudienceToInternalContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChucVuID",
                table: "ThongBaoNoiBo",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChucVuID",
                table: "TaiLieuNoiBo",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThongBaoNoiBo_ChucVuID",
                table: "ThongBaoNoiBo",
                column: "ChucVuID");

            migrationBuilder.CreateIndex(
                name: "IX_TaiLieuNoiBo_ChucVuID",
                table: "TaiLieuNoiBo",
                column: "ChucVuID");

            migrationBuilder.AddForeignKey(
                name: "FK_TaiLieuNoiBo_ChucVu_ChucVuID",
                table: "TaiLieuNoiBo",
                column: "ChucVuID",
                principalTable: "ChucVu",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ThongBaoNoiBo_ChucVu_ChucVuID",
                table: "ThongBaoNoiBo",
                column: "ChucVuID",
                principalTable: "ChucVu",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaiLieuNoiBo_ChucVu_ChucVuID",
                table: "TaiLieuNoiBo");

            migrationBuilder.DropForeignKey(
                name: "FK_ThongBaoNoiBo_ChucVu_ChucVuID",
                table: "ThongBaoNoiBo");

            migrationBuilder.DropIndex(
                name: "IX_ThongBaoNoiBo_ChucVuID",
                table: "ThongBaoNoiBo");

            migrationBuilder.DropIndex(
                name: "IX_TaiLieuNoiBo_ChucVuID",
                table: "TaiLieuNoiBo");

            migrationBuilder.DropColumn(
                name: "ChucVuID",
                table: "ThongBaoNoiBo");

            migrationBuilder.DropColumn(
                name: "ChucVuID",
                table: "TaiLieuNoiBo");
        }
    }
}
