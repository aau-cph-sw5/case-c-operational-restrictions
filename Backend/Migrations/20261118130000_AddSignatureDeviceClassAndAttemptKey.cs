using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSignatureDeviceClassAndAttemptKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing signatures keep DeviceClass 0 (Unknown) and an empty user agent: nothing
            // was recorded about their device.
            migrationBuilder.AddColumn<Guid>(
                name: "AttemptKey",
                table: "Signature",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeviceClass",
                table: "Signature",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "Signature",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttemptKey",
                table: "Signature");

            migrationBuilder.DropColumn(
                name: "DeviceClass",
                table: "Signature");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "Signature");
        }
    }
}
