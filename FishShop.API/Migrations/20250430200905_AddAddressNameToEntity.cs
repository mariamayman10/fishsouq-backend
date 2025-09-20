using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishShop.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressNameToEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "UserAddresses",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "UserAddresses");
        }
    }
}
