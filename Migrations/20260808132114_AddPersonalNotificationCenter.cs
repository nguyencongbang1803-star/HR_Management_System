using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNS.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalNotificationCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ThongBaoCaNhan",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienID = table.Column<int>(type: "int", nullable: false),
                    Loai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MaThamChieu = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DuongDan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DaDoc = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayDoc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBaoCaNhan", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ThongBaoCaNhan_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ThongBaoCaNhan_NhanVienID_MaThamChieu",
                table: "ThongBaoCaNhan",
                columns: new[] { "NhanVienID", "MaThamChieu" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThongBaoCaNhan");
        }
    }
}
