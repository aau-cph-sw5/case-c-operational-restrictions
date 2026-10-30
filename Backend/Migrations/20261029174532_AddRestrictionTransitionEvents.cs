using System;
using System.Collections.Generic;

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
            migrationBuilder.DropPrimaryKey(
                name: "PK_OperationalRestrictions",
                table: "OperationalRestrictions");

            // Renames corrected by hand: EF's scaffold paired Id/Originator/Status
            // with the wrong new columns.
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "OperationalRestrictions",
                newName: "RestrictionId");

            migrationBuilder.RenameColumn(
                name: "Originator",
                table: "OperationalRestrictions",
                newName: "OriginatorUserId");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "OperationalRestrictions",
                newName: "State");

            // RestrictionStatus -> RestrictionState have different integer values.
            migrationBuilder.Sql(
                """
                UPDATE "OperationalRestrictions" SET "State" = CASE "State"
                    WHEN 0 THEN 0  -- Draft -> Draft
                    WHEN 1 THEN 1  -- PendingApproval -> PendingDutyOperationsManagerApproval
                    WHEN 2 THEN 4  -- Approved -> Approved
                    WHEN 3 THEN 6  -- Active -> Active
                    WHEN 4 THEN 12 -- Archived -> Archived
                    WHEN 5 THEN 3  -- Cancelled -> CancelledRefused
                    WHEN 6 THEN 8  -- CLOSING -> PendingCancellation
                    ELSE 0
                END;
                """);

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

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "OperationalRestrictions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ArchivedAt",
                table: "OperationalRestrictions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "OperationalRestrictions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewerId",
                table: "OperationalRestrictions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "ApprovedForSigning",
                table: "OperationalRestrictions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<List<Guid>>(
                name: "AssignedOperatorsIds",
                table: "OperationalRestrictions",
                type: "uuid[]",
                nullable: false,
                defaultValueSql: "'{}'::uuid[]");

            migrationBuilder.AddColumn<string>(
                name: "RestrictionDescription",
                table: "OperationalRestrictions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "RetentionUntil",
                table: "OperationalRestrictions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewDescription",
                table: "OperationalRestrictions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "OperationalRestrictions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OperationalRestrictions",
                table: "OperationalRestrictions",
                column: "RestrictionId");

            migrationBuilder.CreateTable(
                name: "File",
                columns: table => new
                {
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    StoragePath = table.Column<string>(type: "text", nullable: false),
                    RestrictionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OperationalRestrictionRestrictionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_File", x => x.FileId);
                    table.ForeignKey(
                        name: "FK_File_OperationalRestrictions_OperationalRestrictionRestrict~",
                        column: x => x.OperationalRestrictionRestrictionId,
                        principalTable: "OperationalRestrictions",
                        principalColumn: "RestrictionId");
                });

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

            migrationBuilder.CreateTable(
                name: "Signature",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiresSignatureId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationalRestrictionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SigneeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SigneeRole = table.Column<int>(type: "integer", nullable: false),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Signature", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Signature_OperationalRestrictions_OperationalRestrictionId",
                        column: x => x.OperationalRestrictionId,
                        principalTable: "OperationalRestrictions",
                        principalColumn: "RestrictionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_File_OperationalRestrictionRestrictionId",
                table: "File",
                column: "OperationalRestrictionRestrictionId");

            migrationBuilder.CreateIndex(
                name: "IX_Signature_OperationalRestrictionId",
                table: "Signature",
                column: "OperationalRestrictionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "File");

            migrationBuilder.DropTable(
                name: "RestrictionTransitionEvents");

            migrationBuilder.DropTable(
                name: "Signature");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OperationalRestrictions",
                table: "OperationalRestrictions");

            migrationBuilder.DropColumn(
                name: "InCharge",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WorkingStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "OperationalRestrictions");

            migrationBuilder.DropColumn(
                name: "ReviewerId",
                table: "OperationalRestrictions");

            migrationBuilder.DropColumn(
                name: "ApprovedForSigning",
                table: "OperationalRestrictions");

            migrationBuilder.DropColumn(
                name: "AssignedOperatorsIds",
                table: "OperationalRestrictions");

            migrationBuilder.DropColumn(
                name: "RestrictionDescription",
                table: "OperationalRestrictions");

            migrationBuilder.DropColumn(
                name: "RetentionUntil",
                table: "OperationalRestrictions");

            migrationBuilder.DropColumn(
                name: "ReviewDescription",
                table: "OperationalRestrictions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "OperationalRestrictions");

            // States without a RestrictionStatus equivalent fall back to Draft.
            migrationBuilder.Sql(
                """
                UPDATE "OperationalRestrictions" SET "State" = CASE "State"
                    WHEN 0 THEN 0
                    WHEN 1 THEN 1
                    WHEN 4 THEN 2
                    WHEN 6 THEN 3
                    WHEN 12 THEN 4
                    WHEN 3 THEN 5
                    WHEN 8 THEN 6
                    ELSE 0
                END;
                """);

            migrationBuilder.RenameColumn(
                name: "State",
                table: "OperationalRestrictions",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "OriginatorUserId",
                table: "OperationalRestrictions",
                newName: "Originator");

            migrationBuilder.RenameColumn(
                name: "RestrictionId",
                table: "OperationalRestrictions",
                newName: "Id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "OperationalRestrictions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ArchivedAt",
                table: "OperationalRestrictions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OperationalRestrictions",
                table: "OperationalRestrictions",
                column: "Id");
        }
    }
}
