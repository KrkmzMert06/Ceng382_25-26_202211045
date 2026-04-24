using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaterFlow.Migrations
{
    /// <inheritdoc />
    public partial class FixWeek2Customization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "MenuItems",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "MenuItemCustomizationGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuItemId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    AllowsMultipleSelection = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemCustomizationGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItemCustomizationGroups_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemCustomizationOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuItemCustomizationGroupId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PriceModifier = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsRemovableIngredient = table.Column<bool>(type: "bit", nullable: false),
                    IsDefaultSelected = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemCustomizationOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItemCustomizationOptions_MenuItemCustomizationGroups_MenuItemCustomizationGroupId",
                        column: x => x.MenuItemCustomizationGroupId,
                        principalTable: "MenuItemCustomizationGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemCustomizationGroups_MenuItemId",
                table: "MenuItemCustomizationGroups",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemCustomizationOptions_MenuItemCustomizationGroupId",
                table: "MenuItemCustomizationOptions",
                column: "MenuItemCustomizationGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MenuItemCustomizationOptions");

            migrationBuilder.DropTable(
                name: "MenuItemCustomizationGroups");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "MenuItems",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250);
        }
    }
}
