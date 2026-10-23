using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNS.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveApprovalEvidenceAndPayAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContentTypeMinhChung",
                table: "NghiPhep",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DuongDanMinhChung",
                table: "NghiPhep",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayDuyetQuanLy",
                table: "NghiPhep",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiDuyetQuanLy",
                table: "NghiPhep",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhanHoiQuanLy",
                table: "NghiPhep",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoNgayHuongLuong",
                table: "NghiPhep",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoNgayKhongLuong",
                table: "NghiPhep",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TenFileMinhChung",
                table: "NghiPhep",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentTypeMinhChung",
                table: "NghiPhep");

            migrationBuilder.DropColumn(
                name: "DuongDanMinhChung",
                table: "NghiPhep");

            migrationBuilder.DropColumn(
                name: "NgayDuyetQuanLy",
                table: "NghiPhep");

            migrationBuilder.DropColumn(
                name: "NguoiDuyetQuanLy",
                table: "NghiPhep");

            migrationBuilder.DropColumn(
                name: "PhanHoiQuanLy",
                table: "NghiPhep");

            migrationBuilder.DropColumn(
                name: "SoNgayHuongLuong",
                table: "NghiPhep");

            migrationBuilder.DropColumn(
                name: "SoNgayKhongLuong",
                table: "NghiPhep");

            migrationBuilder.DropColumn(
                name: "TenFileMinhChung",
                table: "NghiPhep");
        }
    }
}
