using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRequiresSignatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequiresSignatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationalRestrictionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SigneeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequiresSignatureStatus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequiresSignatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequiresSignatures_OperationalRestrictions_OperationalRestr~",
                        column: x => x.OperationalRestrictionId,
                        principalTable: "OperationalRestrictions",
                        principalColumn: "RestrictionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequiresSignatures_Users_SigneeId",
                        column: x => x.SigneeId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequiresSignatures_OperationalRestrictionId",
                table: "RequiresSignatures",
                column: "OperationalRestrictionId");

            migrationBuilder.CreateIndex(
                name: "IX_RequiresSignatures_SigneeId",
                table: "RequiresSignatures",
                column: "SigneeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequiresSignatures");
        }
    }
}
