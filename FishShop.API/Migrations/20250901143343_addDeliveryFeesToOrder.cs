using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishShop.API.Migrations
{
    /// <inheritdoc />
    public partial class addDeliveryFeesToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryFees",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryFees",
                table: "Orders");
        }
    }
}
