using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBookingAppWebApi.Migrations
{
    /// <inheritdoc />
    public partial class PromptChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amenities",
                table: "RoomTypes");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Hotels",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State",
                table: "Hotels");

            migrationBuilder.AddColumn<string>(
                name: "Amenities",
                table: "RoomTypes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
