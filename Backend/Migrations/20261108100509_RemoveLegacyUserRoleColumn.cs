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
            // Carry each user's legacy Role enum value over into UserRoleAssignments
            // before the column is dropped. Migrations run before seeding, so the
            // Roles table may still be empty here; insert the five well-known roles
            // with fixed Ids (the seeder matches roles by name and will not duplicate them).
            migrationBuilder.Sql(
                """
                INSERT INTO "Roles" ("Id", "Name", "Description")
                VALUES
                    ('10000000-0000-0000-0000-000000000001', 'Originator', NULL),
                    ('10000000-0000-0000-0000-000000000002', 'DutyOperationsManager', NULL),
                    ('10000000-0000-0000-0000-000000000003', 'Operator', NULL),
                    ('10000000-0000-0000-0000-000000000004', 'ControlroomSupervisor', NULL),
                    ('10000000-0000-0000-0000-000000000005', 'Betriebsleiter', NULL)
                ON CONFLICT ("Name") DO NOTHING;
                """);

            // The old UserRole enum values map 1:1 onto the role names.
            migrationBuilder.Sql(
                """
                INSERT INTO "UserRoleAssignments" ("UserId", "RoleId")
                SELECT u."Id", r."Id"
                FROM "Users" u
                JOIN "Roles" r ON r."Name" = CASE u."Role"
                    WHEN 0 THEN 'Originator'
                    WHEN 1 THEN 'DutyOperationsManager'
                    WHEN 2 THEN 'Operator'
                    WHEN 3 THEN 'ControlroomSupervisor'
                    WHEN 4 THEN 'Betriebsleiter'
                END
                ON CONFLICT ("UserId", "RoleId") DO NOTHING;
                """);

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
