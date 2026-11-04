using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUserWorkingStatusAndInCharge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InCharge",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "WorkingStatus",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InCharge",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WorkingStatus",
                table: "Users");
        }
    }
}
