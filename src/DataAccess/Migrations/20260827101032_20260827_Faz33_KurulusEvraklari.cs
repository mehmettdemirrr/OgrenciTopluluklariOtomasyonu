using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class _20260827_Faz33_KurulusEvraklari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClubDocumentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubDocumentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClubApplicationDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubApplicationId = table.Column<int>(type: "int", nullable: false),
                    ClubDocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    StoredFileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubApplicationDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubApplicationDocuments_ClubApplications_ClubApplicationId",
                        column: x => x.ClubApplicationId,
                        principalTable: "ClubApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClubApplicationDocuments_ClubDocumentTypes_ClubDocumentTypeId",
                        column: x => x.ClubDocumentTypeId,
                        principalTable: "ClubDocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClubApplicationDocuments_StoredFiles_StoredFileId",
                        column: x => x.StoredFileId,
                        principalTable: "StoredFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ClubDocumentTypes",
                columns: new[] { "Id", "Code", "DisplayOrder", "IsActive", "IsRequired", "Name" },
                values: new object[,]
                {
                    { 1, "FR-0230", 1, true, true, "Topluluk Akademik Danışman Dilekçesi" },
                    { 2, "FR-0240", 2, true, true, "Topluluk Asıl Üyeler (Yönetim Kurulu)" },
                    { 3, "FR-0241", 3, true, true, "Topluluk Faaliyet Planı" },
                    { 4, "FR-0242", 4, true, true, "Topluluk Kapak Sayfası" },
                    { 5, "FR-0243", 5, true, true, "Topluluk Kurucu Üye Dilekçesi" },
                    { 6, "FR-0244", 6, true, true, "Topluluk Kuruluş Dilekçesi" },
                    { 7, "FR-0245", 7, true, true, "Topluluk Üye Listesi" },
                    { 8, "FR-0272", 8, true, true, "Topluluk Örnek Tüzük" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClubApplicationDocuments_ClubApplicationId_ClubDocumentTypeId",
                table: "ClubApplicationDocuments",
                columns: new[] { "ClubApplicationId", "ClubDocumentTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubApplicationDocuments_ClubDocumentTypeId",
                table: "ClubApplicationDocuments",
                column: "ClubDocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubApplicationDocuments_StoredFileId",
                table: "ClubApplicationDocuments",
                column: "StoredFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubDocumentTypes_Code",
                table: "ClubDocumentTypes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClubApplicationDocuments");

            migrationBuilder.DropTable(
                name: "ClubDocumentTypes");
        }
    }
}
