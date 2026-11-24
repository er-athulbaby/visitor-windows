using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceGatePass.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitVehicleNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VehicleNo",
                table: "Visits",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VehicleNo",
                table: "Visits");
        }
    }
}
