using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QLNS.Data;

#nullable disable

namespace QLNS.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260723093000_RemoveIndefiniteContractType")]
public partial class RemoveIndefiniteContractType : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Chỉ xóa khỏi danh mục lựa chọn; dữ liệu trên các hợp đồng cũ vẫn được giữ nguyên.
        migrationBuilder.Sql(
            """
            DELETE FROM [LoaiHopDong]
            WHERE LOWER(LTRIM(RTRIM([TenLoai]))) IN (
                N'không xác định thời hạn',
                N'vô thời hạn',
                N'không thời hạn'
            );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF NOT EXISTS (
                SELECT 1 FROM [LoaiHopDong]
                WHERE [TenLoai] = N'Không xác định thời hạn'
            )
            BEGIN
                INSERT INTO [LoaiHopDong] ([TenLoai])
                VALUES (N'Không xác định thời hạn');
            END;
            """);
    }
}
