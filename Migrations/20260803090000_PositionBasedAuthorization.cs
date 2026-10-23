using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QLNS.Data;

#nullable disable

namespace QLNS.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260803090000_PositionBasedAuthorization")]
public partial class PositionBasedAuthorization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PhanQuyen",
            table: "ChucVu",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "NhanVien");

        migrationBuilder.Sql("""
            UPDATE ChucVu SET PhanQuyen = 'Admin'
            WHERE Chucvu = N'Giám đốc';

            UPDATE ChucVu SET PhanQuyen = 'QuanLy'
            WHERE Chucvu IN (N'Trưởng phòng', N'Phó giám đốc');

            UPDATE ChucVu SET PhanQuyen = 'HR'
            WHERE Chucvu = N'Chuyên viên nhân sự';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "PhanQuyen",
            table: "ChucVu");
    }
}
