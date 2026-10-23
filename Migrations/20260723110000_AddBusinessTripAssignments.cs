using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QLNS.Data;

#nullable disable

namespace QLNS.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260723110000_AddBusinessTripAssignments")]
public partial class AddBusinessTripAssignments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_CongTac_NhanVien_NhanVienID",
            table: "CongTac");

        migrationBuilder.AlterColumn<int>(
            name: "NhanVienID",
            table: "CongTac",
            type: "int",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "int");

        migrationBuilder.AddForeignKey(
            name: "FK_CongTac_NhanVien_NhanVienID",
            table: "CongTac",
            column: "NhanVienID",
            principalTable: "NhanVien",
            principalColumn: "ID",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.CreateTable(
            name: "PhanCongCongTac",
            columns: table => new
            {
                ID = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CongTacID = table.Column<int>(type: "int", nullable: false),
                NhanVienID = table.Column<int>(type: "int", nullable: false),
                NgayThongBao = table.Column<DateTime>(type: "datetime2", nullable: false),
                DaXem = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PhanCongCongTac", x => x.ID);
                table.ForeignKey(
                    name: "FK_PhanCongCongTac_CongTac_CongTacID",
                    column: x => x.CongTacID,
                    principalTable: "CongTac",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_PhanCongCongTac_NhanVien_NhanVienID",
                    column: x => x.NhanVienID,
                    principalTable: "NhanVien",
                    principalColumn: "ID",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.Sql(
            """
            INSERT INTO [PhanCongCongTac] ([CongTacID], [NhanVienID], [NgayThongBao], [DaXem])
            SELECT [ID], [NhanVienID], COALESCE([NgayDuyet], [NgayTao]), 0
            FROM [CongTac]
            WHERE [NhanVienID] IS NOT NULL
              AND [TrangThai] <> N'Rejected';

            UPDATE [CongTac]
            SET [TrangThai] = N'Approved',
                [NgayDuyet] = COALESCE([NgayDuyet], SYSUTCDATETIME()),
                [NguoiDuyet] = COALESCE([NguoiDuyet], N'Chuyển đổi hệ thống')
            WHERE [TrangThai] = N'Pending';

            DELETE FROM [QuyTrinhPheDuyet]
            WHERE [LoaiYeuCau] = N'CongTac';
            """);

        migrationBuilder.CreateIndex(
            name: "IX_PhanCongCongTac_NhanVienID",
            table: "PhanCongCongTac",
            column: "NhanVienID");

        migrationBuilder.CreateIndex(
            name: "IX_PhanCongCongTac_CongTacID_NhanVienID",
            table: "PhanCongCongTac",
            columns: new[] { "CongTacID", "NhanVienID" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PhanCongCongTac");

        migrationBuilder.Sql(
            """
            IF NOT EXISTS (SELECT 1 FROM [QuyTrinhPheDuyet] WHERE [LoaiYeuCau] = N'CongTac')
            BEGIN
                INSERT INTO [QuyTrinhPheDuyet] ([LoaiYeuCau], [TenQuyTrinh], [HoatDong])
                VALUES (N'CongTac', N'Duyệt đề nghị công tác', 1);
            END;
            """);

        migrationBuilder.DropForeignKey(
            name: "FK_CongTac_NhanVien_NhanVienID",
            table: "CongTac");

        migrationBuilder.AlterColumn<int>(
            name: "NhanVienID",
            table: "CongTac",
            type: "int",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.AddForeignKey(
            name: "FK_CongTac_NhanVien_NhanVienID",
            table: "CongTac",
            column: "NhanVienID",
            principalTable: "NhanVien",
            principalColumn: "ID",
            onDelete: ReferentialAction.Cascade);
    }
}
