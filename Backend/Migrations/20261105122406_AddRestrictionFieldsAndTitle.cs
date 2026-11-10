using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRestrictionFieldsAndTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationDescription",
                table: "OperationalRestrictions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "OperationalRestrictions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SituationDescription",
                table: "OperationalRestrictions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "OperationalRestrictions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationDescription",
                table: "OperationalRestrictions");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "OperationalRestrictions");

            migrationBuilder.DropColumn(
                name: "SituationDescription",
                table: "OperationalRestrictions");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "OperationalRestrictions");
        }
    }
}
