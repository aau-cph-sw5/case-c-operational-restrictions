using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueRestrictionVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OperationalRestrictions_OriginalOperationalRestrictionId_Ve~",
                table: "OperationalRestrictions",
                columns: new[] { "OriginalOperationalRestrictionId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OperationalRestrictions_OriginalOperationalRestrictionId_Ve~",
                table: "OperationalRestrictions");
        }
    }
}
