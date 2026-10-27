using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateColumnNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "State",
                table: "OperationalRestrictions",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "OriginatorUserId",
                table: "OperationalRestrictions",
                newName: "Originator");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Status",
                table: "OperationalRestrictions",
                newName: "State");

            migrationBuilder.RenameColumn(
                name: "Originator",
                table: "OperationalRestrictions",
                newName: "OriginatorUserId");
        }
    }
}
