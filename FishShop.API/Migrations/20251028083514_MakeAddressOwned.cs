using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishShop.API.Migrations
{
    /// <inheritdoc />
    public partial class MakeAddressOwned : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "PromoCode",
                table: "Orders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Address_ApartmentNumber",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_Area",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_BuildingNumber",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_FloorNumber",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_Governorate",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address_Street",
                table: "Orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address_ApartmentNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Address_Area",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Address_BuildingNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Address_FloorNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Address_Governorate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Address_Street",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "PromoCode",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressId",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
