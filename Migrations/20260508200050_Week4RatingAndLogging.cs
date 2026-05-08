using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaterFlow.Migrations
{
    /// <inheritdoc />
    public partial class Week4RatingAndLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Ratings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    MenuItemId = table.Column<int>(type: "int", nullable: true),
                    CatererProfileId = table.Column<int>(type: "int", nullable: true),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ratings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ratings_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ratings_CatererProfiles_CatererProfileId",
                        column: x => x.CatererProfileId,
                        principalTable: "CatererProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Ratings_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Ratings_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SystemLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AppUserId = table.Column<int>(type: "int", nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Level = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_AppUserId",
                table: "Ratings",
                column: "AppUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_CatererProfileId",
                table: "Ratings",
                column: "CatererProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_MenuItemId",
                table: "Ratings",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_OrderId",
                table: "Ratings",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_CreatedAt",
                table: "SystemLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_EventType",
                table: "SystemLogs",
                column: "EventType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ratings");

            migrationBuilder.DropTable(
                name: "SystemLogs");
        }
    }
}
