using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestChangesGrant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO "Permissions" ("Id", "Name", "Description")
                VALUES ('20000000-0000-0000-0000-000000000001', 'OperationalRestriction:RequestChanges', 'Request changes to operational restrictions')
                ON CONFLICT ("Name") DO NOTHING;

                INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
                SELECT r."Id", p."Id"
                FROM "Roles" r CROSS JOIN "Permissions" p
                WHERE r."Name" = 'DutyOperationsManager' AND p."Name" = 'OperationalRestriction:RequestChanges'
                ON CONFLICT ("RoleId", "PermissionId") DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "Permissions"
                WHERE "Id" = '20000000-0000-0000-0000-000000000001'
                  AND "Name" = 'OperationalRestriction:RequestChanges';
                """);
        }
    }
}
