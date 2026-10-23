using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNS.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionAnnualLeaveQuota : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SoNgayPhepNam",
                table: "ChucVu",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 16m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SoNgayPhepNam",
                table: "ChucVu");
        }
    }
}
