using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelBookingAppWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationFeeAndRefundFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CancellationFeeAmount",
                table: "Reservations",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "CancellationFeePaid",
                table: "Reservations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "RefundRequests",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RefundNote",
                table: "RefundRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationFeeAmount",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CancellationFeePaid",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "RefundRequests");

            migrationBuilder.DropColumn(
                name: "RefundNote",
                table: "RefundRequests");
        }
    }
}
