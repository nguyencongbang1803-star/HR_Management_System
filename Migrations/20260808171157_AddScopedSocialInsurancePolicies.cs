using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNS.Migrations
{
    /// <inheritdoc />
    public partial class AddScopedSocialInsurancePolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChinhSachBHXH",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenKhoan = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SoTienDong = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PhamVi = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PhongBanID = table.Column<int>(type: "int", nullable: true),
                    ChucVuID = table.Column<int>(type: "int", nullable: true),
                    NhanVienID = table.Column<int>(type: "int", nullable: true),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KieuThoiHan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SoThang = table.Column<int>(type: "int", nullable: true),
                    HoatDong = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChinhSachBHXH", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ChinhSachBHXH_ChucVu_ChucVuID",
                        column: x => x.ChucVuID,
                        principalTable: "ChucVu",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChinhSachBHXH_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChinhSachBHXH_PhongBan_PhongBanID",
                        column: x => x.PhongBanID,
                        principalTable: "PhongBan",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChinhSachBHXH_ChucVuID",
                table: "ChinhSachBHXH",
                column: "ChucVuID");

            migrationBuilder.CreateIndex(
                name: "IX_ChinhSachBHXH_NhanVienID",
                table: "ChinhSachBHXH",
                column: "NhanVienID");

            migrationBuilder.CreateIndex(
                name: "IX_ChinhSachBHXH_PhamVi_HoatDong",
                table: "ChinhSachBHXH",
                columns: new[] { "PhamVi", "HoatDong" });

            migrationBuilder.CreateIndex(
                name: "IX_ChinhSachBHXH_PhongBanID",
                table: "ChinhSachBHXH",
                column: "PhongBanID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChinhSachBHXH");
        }
    }
}
