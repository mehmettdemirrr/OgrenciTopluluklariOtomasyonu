using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class _20260910_KurulusEvrakiSablonu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TemplateFileId",
                table: "ClubDocumentTypes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubDocumentTypes_TemplateFileId",
                table: "ClubDocumentTypes",
                column: "TemplateFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClubDocumentTypes_StoredFiles_TemplateFileId",
                table: "ClubDocumentTypes",
                column: "TemplateFileId",
                principalTable: "StoredFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubDocumentTypes_StoredFiles_TemplateFileId",
                table: "ClubDocumentTypes");

            migrationBuilder.DropIndex(
                name: "IX_ClubDocumentTypes_TemplateFileId",
                table: "ClubDocumentTypes");

            migrationBuilder.DropColumn(
                name: "TemplateFileId",
                table: "ClubDocumentTypes");
        }
    }
}
