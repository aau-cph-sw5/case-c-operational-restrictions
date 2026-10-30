using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperationalRestrictions",
                columns: table => new
                {
                    RestrictionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginatorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetentionUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RestrictionDescription = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedForSigning = table.Column<bool>(type: "boolean", nullable: false),
                    ReviewDescription = table.Column<string>(type: "text", nullable: false),
                    AssignedOperatorsIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OriginalOperationalRestrictionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalRestrictions", x => x.RestrictionId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserStatus = table.Column<int>(type: "integer", nullable: false),
                    WorkingStatus = table.Column<int>(type: "integer", nullable: false),
                    ReportsTo = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

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
                name: "Signature",
                columns: table => new
                {
                    SignatureId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsSigned = table.Column<bool>(type: "boolean", nullable: false),
                    SigneeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SigneeRole = table.Column<int>(type: "integer", nullable: false),
                    RestrictionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OperationalRestrictionRestrictionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Signature", x => x.SignatureId);
                    table.ForeignKey(
                        name: "FK_Signature_OperationalRestrictions_OperationalRestrictionRes~",
                        column: x => x.OperationalRestrictionRestrictionId,
                        principalTable: "OperationalRestrictions",
                        principalColumn: "RestrictionId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_File_OperationalRestrictionRestrictionId",
                table: "File",
                column: "OperationalRestrictionRestrictionId");

            migrationBuilder.CreateIndex(
                name: "IX_Signature_OperationalRestrictionRestrictionId",
                table: "Signature",
                column: "OperationalRestrictionRestrictionId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "File");

            migrationBuilder.DropTable(
                name: "Signature");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "OperationalRestrictions");
        }
    }
}
