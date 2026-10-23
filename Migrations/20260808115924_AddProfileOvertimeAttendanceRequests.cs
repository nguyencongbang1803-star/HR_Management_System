using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNS.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileOvertimeAttendanceRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChamCongID",
                table: "DieuChinhChamCong",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentTypeMinhChung",
                table: "DangKyTangCa",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DuongDanMinhChung",
                table: "DangKyTangCa",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenFileMinhChung",
                table: "DangKyTangCa",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DieuChinhChamCongMinhChung",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DieuChinhChamCongID = table.Column<int>(type: "int", nullable: false),
                    TenFileGoc = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DuongDan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KichThuoc = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DieuChinhChamCongMinhChung", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DieuChinhChamCongMinhChung_DieuChinhChamCong_DieuChinhChamCongID",
                        column: x => x.DieuChinhChamCongID,
                        principalTable: "DieuChinhChamCong",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "YeuCauCapNhatThongTinCaNhan",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienID = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SDT = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GioiTinh = table.Column<bool>(type: "bit", nullable: false),
                    CanCuocCongDan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SoTaiKhoan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    MaSoThue = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PhanHoi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NguoiDuyet = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayDuyet = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauCapNhatThongTinCaNhan", x => x.ID);
                    table.ForeignKey(
                        name: "FK_YeuCauCapNhatThongTinCaNhan_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DieuChinhChamCong_ChamCongID",
                table: "DieuChinhChamCong",
                column: "ChamCongID");

            migrationBuilder.CreateIndex(
                name: "IX_DieuChinhChamCongMinhChung_DieuChinhChamCongID",
                table: "DieuChinhChamCongMinhChung",
                column: "DieuChinhChamCongID");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauCapNhatThongTinCaNhan_NhanVienID_TrangThai",
                table: "YeuCauCapNhatThongTinCaNhan",
                columns: new[] { "NhanVienID", "TrangThai" });

            migrationBuilder.AddForeignKey(
                name: "FK_DieuChinhChamCong_ChamCong_ChamCongID",
                table: "DieuChinhChamCong",
                column: "ChamCongID",
                principalTable: "ChamCong",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DieuChinhChamCong_ChamCong_ChamCongID",
                table: "DieuChinhChamCong");

            migrationBuilder.DropTable(
                name: "DieuChinhChamCongMinhChung");

            migrationBuilder.DropTable(
                name: "YeuCauCapNhatThongTinCaNhan");

            migrationBuilder.DropIndex(
                name: "IX_DieuChinhChamCong_ChamCongID",
                table: "DieuChinhChamCong");

            migrationBuilder.DropColumn(
                name: "ChamCongID",
                table: "DieuChinhChamCong");

            migrationBuilder.DropColumn(
                name: "ContentTypeMinhChung",
                table: "DangKyTangCa");

            migrationBuilder.DropColumn(
                name: "DuongDanMinhChung",
                table: "DangKyTangCa");

            migrationBuilder.DropColumn(
                name: "TenFileMinhChung",
                table: "DangKyTangCa");
        }
    }
}
