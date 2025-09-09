using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishShop.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminPrivileges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminPrivileges",
                columns: table => new
                {
                    AdminId = table.Column<string>(type: "text", nullable: false),
                    CanAddProduct = table.Column<bool>(type: "boolean", nullable: false),
                    CanUpdateProduct = table.Column<bool>(type: "boolean", nullable: false),
                    CanDeleteProduct = table.Column<bool>(type: "boolean", nullable: false),
                    CanAddCategory = table.Column<bool>(type: "boolean", nullable: false),
                    CanUpdateCategory = table.Column<bool>(type: "boolean", nullable: false),
                    CanDeleteCategory = table.Column<bool>(type: "boolean", nullable: false),
                    CanUpdateOrderStatus = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminPrivileges", x => x.AdminId);
                    table.ForeignKey(
                        name: "FK_AdminPrivileges_AspNetUsers_AdminId",
                        column: x => x.AdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminPrivileges");
        }
    }
}
