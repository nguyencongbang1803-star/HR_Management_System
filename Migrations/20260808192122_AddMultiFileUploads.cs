using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNS.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiFileUploads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DangKyTangCaMinhChung",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DangKyTangCaID = table.Column<int>(type: "int", nullable: false),
                    TenFileGoc = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    DuongDan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KichThuoc = table.Column<long>(type: "bigint", nullable: false),
                    NgayTaiLen = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DangKyTangCaMinhChung", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DangKyTangCaMinhChung_DangKyTangCa_DangKyTangCaID",
                        column: x => x.DangKyTangCaID,
                        principalTable: "DangKyTangCa",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NghiPhepMinhChung",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NghiPhepID = table.Column<int>(type: "int", nullable: false),
                    TenFileGoc = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    DuongDan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KichThuoc = table.Column<long>(type: "bigint", nullable: false),
                    NgayTaiLen = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NghiPhepMinhChung", x => x.ID);
                    table.ForeignKey(
                        name: "FK_NghiPhepMinhChung_NghiPhep_NghiPhepID",
                        column: x => x.NghiPhepID,
                        principalTable: "NghiPhep",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TepTaiLieuNoiBo",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaiLieuNoiBoID = table.Column<int>(type: "int", nullable: false),
                    TenFileGoc = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    DuongDan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KichThuoc = table.Column<long>(type: "bigint", nullable: false),
                    NgayTaiLen = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TepTaiLieuNoiBo", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TepTaiLieuNoiBo_TaiLieuNoiBo_TaiLieuNoiBoID",
                        column: x => x.TaiLieuNoiBoID,
                        principalTable: "TaiLieuNoiBo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO [NghiPhepMinhChung]
                    ([NghiPhepID], [TenFileGoc], [DuongDan], [ContentType], [KichThuoc], [NgayTaiLen])
                SELECT [ID], COALESCE(NULLIF([TenFileMinhChung], ''), N'minh-chung'),
                    [DuongDanMinhChung], COALESCE(NULLIF([ContentTypeMinhChung], ''), 'application/octet-stream'),
                    0, [NgayTao]
                FROM [NghiPhep]
                WHERE NULLIF([DuongDanMinhChung], '') IS NOT NULL;

                INSERT INTO [DangKyTangCaMinhChung]
                    ([DangKyTangCaID], [TenFileGoc], [DuongDan], [ContentType], [KichThuoc], [NgayTaiLen])
                SELECT [ID], COALESCE(NULLIF([TenFileMinhChung], ''), N'minh-chung'),
                    [DuongDanMinhChung], COALESCE(NULLIF([ContentTypeMinhChung], ''), 'application/octet-stream'),
                    0, [NgayTao]
                FROM [DangKyTangCa]
                WHERE NULLIF([DuongDanMinhChung], '') IS NOT NULL;

                INSERT INTO [TepTaiLieuNoiBo]
                    ([TaiLieuNoiBoID], [TenFileGoc], [DuongDan], [ContentType], [KichThuoc], [NgayTaiLen])
                SELECT [ID], COALESCE(NULLIF([TenFileGoc], ''), N'tai-lieu'),
                    [DuongDan], COALESCE(NULLIF([ContentType], ''), 'application/octet-stream'),
                    [KichThuoc], [NgayPhatHanh]
                FROM [TaiLieuNoiBo]
                WHERE NULLIF([DuongDan], '') IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_DangKyTangCaMinhChung_DangKyTangCaID",
                table: "DangKyTangCaMinhChung",
                column: "DangKyTangCaID");

            migrationBuilder.CreateIndex(
                name: "IX_NghiPhepMinhChung_NghiPhepID",
                table: "NghiPhepMinhChung",
                column: "NghiPhepID");

            migrationBuilder.CreateIndex(
                name: "IX_TepTaiLieuNoiBo_TaiLieuNoiBoID",
                table: "TepTaiLieuNoiBo",
                column: "TaiLieuNoiBoID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DangKyTangCaMinhChung");

            migrationBuilder.DropTable(
                name: "NghiPhepMinhChung");

            migrationBuilder.DropTable(
                name: "TepTaiLieuNoiBo");
        }
    }
}
