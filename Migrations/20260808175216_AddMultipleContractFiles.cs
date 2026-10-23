using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNS.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleContractFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaiLieuHopDong",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HopDongID = table.Column<int>(type: "int", nullable: false),
                    TenFileGoc = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    DuongDanFile = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    KichThuoc = table.Column<long>(type: "bigint", nullable: false),
                    NgayTaiLen = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiLieuHopDong", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TaiLieuHopDong_HopDong_HopDongID",
                        column: x => x.HopDongID,
                        principalTable: "HopDong",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaiLieuHopDong_HopDongID",
                table: "TaiLieuHopDong",
                column: "HopDongID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaiLieuHopDong");
        }
    }
}
