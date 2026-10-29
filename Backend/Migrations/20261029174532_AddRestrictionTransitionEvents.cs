using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRestrictionTransitionEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Signature_OperationalRestrictions_OperationalRestrictionRes~",
                table: "Signature");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Signature",
                table: "Signature");

            migrationBuilder.DropIndex(
                name: "IX_Signature_OperationalRestrictionRestrictionId",
                table: "Signature");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UserStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsSigned",
                table: "Signature");

            migrationBuilder.DropColumn(
                name: "OperationalRestrictionRestrictionId",
                table: "Signature");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Signature");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Signature");

            migrationBuilder.RenameColumn(
                name: "ReportsTo",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "RestrictionId",
                table: "Signature",
                newName: "RequiresSignatureId");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Signature",
                newName: "SignedAt");

            migrationBuilder.RenameColumn(
                name: "SignatureId",
                table: "Signature",
                newName: "OperationalRestrictionId");

            migrationBuilder.AddColumn<bool>(
                name: "InCharge",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "Signature",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Signature",
                table: "Signature",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "RestrictionTransitionEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RestrictionVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalRestrictionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    FromState = table.Column<int>(type: "integer", nullable: true),
                    ToState = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestrictionTransitionEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Signature_OperationalRestrictionId",
                table: "Signature",
                column: "OperationalRestrictionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Signature_OperationalRestrictions_OperationalRestrictionId",
                table: "Signature",
                column: "OperationalRestrictionId",
                principalTable: "OperationalRestrictions",
                principalColumn: "RestrictionId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Signature_OperationalRestrictions_OperationalRestrictionId",
                table: "Signature");

            migrationBuilder.DropTable(
                name: "RestrictionTransitionEvents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Signature",
                table: "Signature");

            migrationBuilder.DropIndex(
                name: "IX_Signature_OperationalRestrictionId",
                table: "Signature");

            migrationBuilder.DropColumn(
                name: "InCharge",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Signature");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Users",
                newName: "ReportsTo");

            migrationBuilder.RenameColumn(
                name: "SignedAt",
                table: "Signature",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "RequiresSignatureId",
                table: "Signature",
                newName: "RestrictionId");

            migrationBuilder.RenameColumn(
                name: "OperationalRestrictionId",
                table: "Signature",
                newName: "SignatureId");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserStatus",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsSigned",
                table: "Signature",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OperationalRestrictionRestrictionId",
                table: "Signature",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Signature",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Signature",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Signature",
                table: "Signature",
                column: "SignatureId");

            migrationBuilder.CreateIndex(
                name: "IX_Signature_OperationalRestrictionRestrictionId",
                table: "Signature",
                column: "OperationalRestrictionRestrictionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Signature_OperationalRestrictions_OperationalRestrictionRes~",
                table: "Signature",
                column: "OperationalRestrictionRestrictionId",
                principalTable: "OperationalRestrictions",
                principalColumn: "RestrictionId");
        }
    }
}
