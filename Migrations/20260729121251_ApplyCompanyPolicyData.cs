using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNS.Migrations
{
    /// <inheritdoc />
    public partial class ApplyCompanyPolicyData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE soDu
                SET soDu.SoNgayDuocHuong = 16
                FROM SoDuNghiPhep AS soDu
                INNER JOIN LoaiNghiPhep AS loai ON loai.ID = soDu.LoaiNghiPhepID
                WHERE loai.MaLoai = N'PHEP_NAM'
                  AND soDu.SoNgayDuocHuong = 12;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE soDu
                SET soDu.SoNgayDuocHuong = 12
                FROM SoDuNghiPhep AS soDu
                INNER JOIN LoaiNghiPhep AS loai ON loai.ID = soDu.LoaiNghiPhepID
                WHERE loai.MaLoai = N'PHEP_NAM'
                  AND soDu.SoNgayDuocHuong = 16;
                """);
        }
    }
}
