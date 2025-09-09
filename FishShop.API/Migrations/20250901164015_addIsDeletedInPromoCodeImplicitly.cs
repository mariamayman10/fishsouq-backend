using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FishShop.API.Migrations
{
    /// <inheritdoc />
    public partial class addIsDeletedInPromoCodeImplicitly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PromoCodes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PromoCodes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PromoCodes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PromoCodes");
        }
    }
}
