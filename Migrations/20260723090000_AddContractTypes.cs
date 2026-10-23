using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QLNS.Data;

#nullable disable

namespace QLNS.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260723090000_AddContractTypes")]
public partial class AddContractTypes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LoaiHopDong",
            columns: table => new
            {
                ID = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                TenLoai = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LoaiHopDong", x => x.ID);
            });

        // Dùng SQL trực tiếp để migration độc lập với metadata trong file Designer.
        migrationBuilder.Sql(
            """
            INSERT INTO [LoaiHopDong] ([TenLoai])
            VALUES
                (N'Thử việc'),
                (N'Xác định thời hạn'),
                (N'Không xác định thời hạn'),
                (N'Cộng tác viên');
            """);

        // Giữ lại mọi loại hợp đồng đã được người dùng nhập trước khi có danh mục riêng.
        migrationBuilder.Sql(
            """
            INSERT INTO [LoaiHopDong] ([TenLoai])
            SELECT DISTINCT LTRIM(RTRIM(h.[Loaihopdong]))
            FROM [HopDong] h
            WHERE NULLIF(LTRIM(RTRIM(h.[Loaihopdong])), '') IS NOT NULL
              AND NOT EXISTS (
                  SELECT 1
                  FROM [LoaiHopDong] l
                  WHERE l.[TenLoai] = LTRIM(RTRIM(h.[Loaihopdong]))
              );
            """);

        migrationBuilder.CreateIndex(
            name: "IX_LoaiHopDong_TenLoai",
            table: "LoaiHopDong",
            column: "TenLoai",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LoaiHopDong");
    }
}
