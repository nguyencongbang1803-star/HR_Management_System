using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNS.Migrations
{
    /// <inheritdoc />
    public partial class SyncCurrentLeaveBalancesWithPositionQuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE balance
                SET balance.SoNgayDuocHuong = position.SoNgayPhepNam
                FROM SoDuNghiPhep AS balance
                INNER JOIN NhanVien AS employee ON employee.ID = balance.NhanVienID
                INNER JOIN ChucVu AS position ON position.ID = employee.ChucVuID
                INNER JOIN LoaiNghiPhep AS leaveType ON leaveType.ID = balance.LoaiNghiPhepID
                WHERE leaveType.TruQuyPhep = 1 AND balance.Nam >= YEAR(GETDATE());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không thể khôi phục chính xác hạn mức cũ vì dữ liệu đã được chuẩn hóa theo chức vụ.
        }
    }
}
