using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNS.Migrations
{
    /// <inheritdoc />
    public partial class LockContractTypeCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [HopDong]
                SET [Loaihopdong] = CASE
                    WHEN LOWER([Loaihopdong]) LIKE N'%cộng tác%' THEN N'Hợp đồng cộng tác viên'
                    WHEN LOWER([Loaihopdong]) LIKE N'%thử việc%' THEN N'Hợp đồng thử việc'
                    WHEN LOWER([Loaihopdong]) LIKE N'%đào tạo%' THEN N'Hợp đồng đào tạo nghề'
                    WHEN LOWER([Loaihopdong]) LIKE N'%không xác định thời hạn%'
                      OR LOWER([Loaihopdong]) LIKE N'%vô thời hạn%'
                      OR LOWER([Loaihopdong]) LIKE N'%không thời hạn%'
                        THEN N'Hợp đồng lao động không xác định thời hạn'
                    ELSE N'Hợp đồng lao động xác định thời hạn'
                END;

                UPDATE [HopDong]
                SET [Ngayketthuc] = NULL
                WHERE [Loaihopdong] = N'Hợp đồng lao động không xác định thời hạn';

                DELETE FROM [LoaiHopDong];
                INSERT INTO [LoaiHopDong] ([TenLoai]) VALUES
                    (N'Hợp đồng cộng tác viên'),
                    (N'Hợp đồng thử việc'),
                    (N'Hợp đồng đào tạo nghề'),
                    (N'Hợp đồng lao động không xác định thời hạn'),
                    (N'Hợp đồng lao động xác định thời hạn');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [HopDong]
                SET [Loaihopdong] = CASE
                    WHEN [Loaihopdong] = N'Hợp đồng cộng tác viên' THEN N'Cộng tác viên'
                    WHEN [Loaihopdong] = N'Hợp đồng thử việc' THEN N'Thử việc'
                    WHEN [Loaihopdong] = N'Hợp đồng lao động không xác định thời hạn' THEN N'Không xác định thời hạn'
                    ELSE N'Xác định thời hạn'
                END;

                DELETE FROM [LoaiHopDong];
                INSERT INTO [LoaiHopDong] ([TenLoai]) VALUES
                    (N'Cộng tác viên'),
                    (N'Thử việc'),
                    (N'Xác định thời hạn');
                """);
        }
    }
}
