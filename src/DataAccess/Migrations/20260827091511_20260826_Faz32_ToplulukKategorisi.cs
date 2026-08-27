using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class _20260826_Faz32_ToplulukKategorisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClubCategoryId",
                table: "Clubs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProposedCategoryId",
                table: "ClubApplications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClubCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_ClubCategoryId",
                table: "Clubs",
                column: "ClubCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubApplications_ProposedCategoryId",
                table: "ClubApplications",
                column: "ProposedCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubCategories_Name",
                table: "ClubCategories",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClubApplications_ClubCategories_ProposedCategoryId",
                table: "ClubApplications",
                column: "ProposedCategoryId",
                principalTable: "ClubCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_ClubCategories_ClubCategoryId",
                table: "Clubs",
                column: "ClubCategoryId",
                principalTable: "ClubCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubApplications_ClubCategories_ProposedCategoryId",
                table: "ClubApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_ClubCategories_ClubCategoryId",
                table: "Clubs");

            migrationBuilder.DropTable(
                name: "ClubCategories");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_ClubCategoryId",
                table: "Clubs");

            migrationBuilder.DropIndex(
                name: "IX_ClubApplications_ProposedCategoryId",
                table: "ClubApplications");

            migrationBuilder.DropColumn(
                name: "ClubCategoryId",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "ProposedCategoryId",
                table: "ClubApplications");
        }
    }
}
