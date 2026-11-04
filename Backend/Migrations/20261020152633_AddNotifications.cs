using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        private static readonly string[] DeliveryStatusNextAttemptAtColumns = { "Status", "NextAttemptAt" };

        private static readonly string[] RecipientCreatedAtColumns = { "RecipientUserId", "CreatedAt" };

        private static readonly string[] RecipientReadAtColumns = { "RecipientUserId", "ReadAt" };

        private static readonly string[] RecipientActionResolvedColumns =
            { "RecipientUserId", "RequiresAction", "ResolvedAt" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationalRestrictionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    RequiresAction = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_OperationalRestrictions_OperationalRestrictio~",
                        column: x => x.OperationalRestrictionId,
                        principalTable: "OperationalRestrictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveries_Notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "Notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_NotificationId",
                table: "NotificationDeliveries",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveries_Status_NextAttemptAt",
                table: "NotificationDeliveries",
                columns: DeliveryStatusNextAttemptAtColumns);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_OperationalRestrictionId",
                table: "Notifications",
                column: "OperationalRestrictionId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId_CreatedAt",
                table: "Notifications",
                columns: RecipientCreatedAtColumns);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId_ReadAt",
                table: "Notifications",
                columns: RecipientReadAtColumns);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientUserId_RequiresAction_ResolvedAt",
                table: "Notifications",
                columns: RecipientActionResolvedColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationDeliveries");

            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
