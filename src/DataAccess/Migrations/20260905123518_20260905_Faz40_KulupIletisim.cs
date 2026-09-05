using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class _20260905_Faz40_KulupIletisim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Clubs",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Clubs",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClubSocialLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    Platform = table.Column<int>(type: "int", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubSocialLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubSocialLinks_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClubSocialLinks_ClubId_DisplayOrder",
                table: "ClubSocialLinks",
                columns: new[] { "ClubId", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClubSocialLinks");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Clubs");
        }
    }
}
