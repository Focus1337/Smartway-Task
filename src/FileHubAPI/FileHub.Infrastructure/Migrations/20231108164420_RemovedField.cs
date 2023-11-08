using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovedField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentLength",
                table: "FileMeta");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastModified",
                table: "FileMeta",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastModified",
                table: "FileMeta",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AddColumn<long>(
                name: "ContentLength",
                table: "FileMeta",
                type: "bigint",
                nullable: true);
        }
    }
}
