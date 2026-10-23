using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNS.Migrations
{
    /// <inheritdoc />
    public partial class AlignCompanyHrProcesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LaDotXuat",
                table: "NghiPhep",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ThuNhapKhac",
                table: "Luong",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ThueTNCN",
                table: "Luong",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayDuyetQuanLy",
                table: "DangKyTangCa",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiDuyetQuanLy",
                table: "DangKyTangCa",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhanHoiQuanLy",
                table: "DangKyTangCa",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DanhGiaHieuSuat",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienID = table.Column<int>(type: "int", nullable: false),
                    TuNgay = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DenNgay = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChatLuongCongViec = table.Column<int>(type: "int", nullable: false),
                    TienDoCongViec = table.Column<int>(type: "int", nullable: false),
                    TinhThanHopTac = table.Column<int>(type: "int", nullable: false),
                    KyLuat = table.Column<int>(type: "int", nullable: false),
                    DiemManh = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NoiDungCanCaiThien = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DeXuat = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NguoiDanhGia = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NgayDanhGia = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGiaHieuSuat", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DanhGiaHieuSuat_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "YeuCauNhanLuc",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaYeuCau = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PhongBanID = table.Column<int>(type: "int", nullable: false),
                    ViTriCanBoSung = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    HinhThucBoSung = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MoTaCongViec = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NhanVienNoiBoID = table.Column<int>(type: "int", nullable: true),
                    TenUngVien = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NgayPhongVan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GhiChuXuLy = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NguoiDeXuat = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NguoiXuLy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauNhanLuc", x => x.ID);
                    table.ForeignKey(
                        name: "FK_YeuCauNhanLuc_NhanVien_NhanVienNoiBoID",
                        column: x => x.NhanVienNoiBoID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_YeuCauNhanLuc_PhongBan_PhongBanID",
                        column: x => x.PhongBanID,
                        principalTable: "PhongBan",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DanhGiaHieuSuat_NhanVienID",
                table: "DanhGiaHieuSuat",
                column: "NhanVienID");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauNhanLuc_MaYeuCau",
                table: "YeuCauNhanLuc",
                column: "MaYeuCau",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauNhanLuc_NhanVienNoiBoID",
                table: "YeuCauNhanLuc",
                column: "NhanVienNoiBoID");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauNhanLuc_PhongBanID",
                table: "YeuCauNhanLuc",
                column: "PhongBanID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DanhGiaHieuSuat");

            migrationBuilder.DropTable(
                name: "YeuCauNhanLuc");

            migrationBuilder.DropColumn(
                name: "LaDotXuat",
                table: "NghiPhep");

            migrationBuilder.DropColumn(
                name: "ThuNhapKhac",
                table: "Luong");

            migrationBuilder.DropColumn(
                name: "ThueTNCN",
                table: "Luong");

            migrationBuilder.DropColumn(
                name: "NgayDuyetQuanLy",
                table: "DangKyTangCa");

            migrationBuilder.DropColumn(
                name: "NguoiDuyetQuanLy",
                table: "DangKyTangCa");

            migrationBuilder.DropColumn(
                name: "PhanHoiQuanLy",
                table: "DangKyTangCa");
        }
    }
}
