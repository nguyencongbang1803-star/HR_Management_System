using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNS.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PhongBanChaID",
                table: "PhongBan",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhongBan_PhongBanChaID",
                table: "PhongBan",
                column: "PhongBanChaID");

            migrationBuilder.AddForeignKey(
                name: "FK_PhongBan_PhongBan_PhongBanChaID",
                table: "PhongBan",
                column: "PhongBanChaID",
                principalTable: "PhongBan",
                principalColumn: "ID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhongBan_PhongBan_PhongBanChaID",
                table: "PhongBan");

            migrationBuilder.DropIndex(
                name: "IX_PhongBan_PhongBanChaID",
                table: "PhongBan");

            migrationBuilder.DropColumn(
                name: "PhongBanChaID",
                table: "PhongBan");
        }
    }
}
