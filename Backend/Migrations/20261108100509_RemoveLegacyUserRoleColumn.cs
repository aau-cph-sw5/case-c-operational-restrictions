using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyUserRoleColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            // SigneeRole changes from an enum number to a role-name string.
            // Postgres cannot implicitly cast integer to text, and the old
            // numbers carried no text meaning, so the column is replaced.
            migrationBuilder.DropColumn(
                name: "SigneeRole",
                table: "Signature");

            migrationBuilder.AddColumn<string>(
                name: "SigneeRole",
                table: "Signature",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.DropColumn(
                name: "SigneeRole",
                table: "Signature");

            migrationBuilder.AddColumn<int>(
                name: "SigneeRole",
                table: "Signature",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
