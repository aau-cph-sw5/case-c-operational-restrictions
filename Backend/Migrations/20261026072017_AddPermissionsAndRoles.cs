using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionsAndRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleName",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RoleName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RoleName",
                table: "Users");

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

            migrationBuilder.CreateTable(
                name: "UserRoleAssignment",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleAssignment", x => new { x.UserId, x.RoleName });
                    table.ForeignKey(
                        name: "FK_UserRoleAssignment_Roles_RoleName",
                        column: x => x.RoleName,
                        principalTable: "Roles",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoleAssignment_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleAssignment_RoleName",
                table: "UserRoleAssignment",
                column: "RoleName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRoleAssignment");

            migrationBuilder.DropColumn(
                name: "InCharge",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WorkingStatus",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "RoleName",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleName",
                table: "Users",
                column: "RoleName");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleName",
                table: "Users",
                column: "RoleName",
                principalTable: "Roles",
                principalColumn: "Name",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
