using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class _20260821_Faz10_EtkinlikKatilimiVeDuyurular : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Announcements_Clubs_ClubId",
                table: "Announcements");

            migrationBuilder.AlterColumn<int>(
                name: "ClubId",
                table: "Announcements",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "Visibility",
                table: "Announcements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "AspNetRoleClaims",
                columns: new[] { "Id", "ClaimType", "ClaimValue", "RoleId" },
                values: new object[,]
                {
                    { 32, "permission", "announcements.write", 1 },
                    { 33, "permission", "announcements.global", 1 },
                    { 34, "permission", "announcements.write", 2 },
                    { 35, "permission", "announcements.write", 4 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Announcements_Visibility_PublishedAtUtc",
                table: "Announcements",
                columns: new[] { "Visibility", "PublishedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Announcements_Clubs_ClubId",
                table: "Announcements",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Announcements_Clubs_ClubId",
                table: "Announcements");

            migrationBuilder.DropIndex(
                name: "IX_Announcements_Visibility_PublishedAtUtc",
                table: "Announcements");

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "AspNetRoleClaims",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Announcements");

            migrationBuilder.AlterColumn<int>(
                name: "ClubId",
                table: "Announcements",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Announcements_Clubs_ClubId",
                table: "Announcements",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
