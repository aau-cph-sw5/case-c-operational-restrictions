using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRestrictionFieldsAndNullableEndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "OperationalRestrictions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "CancellationDescription",
                table: "OperationalRestrictions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "OperationalRestrictions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RestrictionDescription",
                table: "OperationalRestrictions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SituationDescription",
                table: "OperationalRestrictions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationDescription",
                table: "OperationalRestrictions");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "OperationalRestrictions");

            migrationBuilder.DropColumn(
                name: "RestrictionDescription",
                table: "OperationalRestrictions");

            migrationBuilder.DropColumn(
                name: "SituationDescription",
                table: "OperationalRestrictions");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "OperationalRestrictions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
