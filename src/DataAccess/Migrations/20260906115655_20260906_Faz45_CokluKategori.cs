using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class _20260906_Faz45_CokluKategori : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClubCategoryAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    ClubCategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubCategoryAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubCategoryAssignments_ClubCategories_ClubCategoryId",
                        column: x => x.ClubCategoryId,
                        principalTable: "ClubCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClubCategoryAssignments_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClubCategoryAssignments_ClubCategoryId",
                table: "ClubCategoryAssignments",
                column: "ClubCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubCategoryAssignments_ClubId_ClubCategoryId",
                table: "ClubCategoryAssignments",
                columns: new[] { "ClubId", "ClubCategoryId" },
                unique: true);

            // A-80: mevcut tekil kategoriler bağ tablosuna taşınır — kolon düşmeden ÖNCE.
            migrationBuilder.Sql(@"
                INSERT INTO ClubCategoryAssignments (ClubId, ClubCategoryId)
                SELECT Id, ClubCategoryId FROM Clubs WHERE ClubCategoryId IS NOT NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_ClubCategories_ClubCategoryId",
                table: "Clubs");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_ClubCategoryId",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "ClubCategoryId",
                table: "Clubs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClubCategoryId",
                table: "Clubs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_ClubCategoryId",
                table: "Clubs",
                column: "ClubCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_ClubCategories_ClubCategoryId",
                table: "Clubs",
                column: "ClubCategoryId",
                principalTable: "ClubCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Bağ tablosundaki kulüp başına ilk kategori geri kolona yazılır (kolon eklendikten sonra).
            migrationBuilder.Sql(@"
                UPDATE c SET c.ClubCategoryId = a.ClubCategoryId
                FROM Clubs c
                CROSS APPLY (SELECT TOP 1 ClubCategoryId FROM ClubCategoryAssignments WHERE ClubId = c.Id ORDER BY Id) a;");

            migrationBuilder.DropTable(
                name: "ClubCategoryAssignments");
        }
    }
}
