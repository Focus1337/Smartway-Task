using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFileGroupAndFileMeta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileGroup",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileGroup_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileMeta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    LastModified = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    ContentLength = table.Column<long>(type: "bigint", nullable: true),
                    FileGroupId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileMeta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileMeta_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileMeta_FileGroup_FileGroupId",
                        column: x => x.FileGroupId,
                        principalTable: "FileGroup",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FileMeta_FileGroup_GroupId",
                        column: x => x.GroupId,
                        principalTable: "FileGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileGroup_UserId",
                table: "FileGroup",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FileMeta_FileGroupId",
                table: "FileMeta",
                column: "FileGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_FileMeta_GroupId",
                table: "FileMeta",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_FileMeta_UserId",
                table: "FileMeta",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileMeta");

            migrationBuilder.DropTable(
                name: "FileGroup");
        }
    }
}
