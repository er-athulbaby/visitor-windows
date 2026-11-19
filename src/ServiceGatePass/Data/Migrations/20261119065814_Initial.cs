using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServiceGatePass.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NameAr = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Visitors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Cpr = table.Column<string>(type: "TEXT", nullable: false),
                    NameEn = table.Column<string>(type: "TEXT", nullable: false),
                    NameAr = table.Column<string>(type: "TEXT", nullable: true),
                    Employer = table.Column<string>(type: "TEXT", nullable: true),
                    LastCompany = table.Column<string>(type: "TEXT", nullable: true),
                    LastLocationId = table.Column<int>(type: "INTEGER", nullable: true),
                    LastNote = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visitors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Visits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VisitorId = table.Column<int>(type: "INTEGER", nullable: false),
                    LocationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Company = table.Column<string>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    BadgeNo = table.Column<int>(type: "INTEGER", nullable: true),
                    SlotNo = table.Column<int>(type: "INTEGER", nullable: true),
                    CheckInAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CheckInByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CheckOutAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CheckOutByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Visits_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Visits_Visitors_VisitorId",
                        column: x => x.VisitorId,
                        principalTable: "Visitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visitors_Cpr",
                table: "Visitors",
                column: "Cpr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visits_BadgeNo",
                table: "Visits",
                column: "BadgeNo",
                unique: true,
                filter: "CheckOutAt IS NULL AND BadgeNo IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_CheckInAt",
                table: "Visits",
                column: "CheckInAt");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_LocationId",
                table: "Visits",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_SlotNo",
                table: "Visits",
                column: "SlotNo",
                unique: true,
                filter: "CheckOutAt IS NULL AND SlotNo IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_VisitorId",
                table: "Visits",
                column: "VisitorId",
                unique: true,
                filter: "CheckOutAt IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Visits");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "Visitors");
        }
    }
}
