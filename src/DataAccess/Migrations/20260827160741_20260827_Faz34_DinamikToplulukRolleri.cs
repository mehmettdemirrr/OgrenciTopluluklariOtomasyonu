using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class _20260827_Faz34_DinamikToplulukRolleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClubRoleDefinitionId",
                table: "ClubMemberships",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClubRoleDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClubRole = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubRoleDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubRoleDefinitions_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClubMemberships_ClubRoleDefinitionId",
                table: "ClubMemberships",
                column: "ClubRoleDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubRoleDefinitions_ClubId_Name",
                table: "ClubRoleDefinitions",
                columns: new[] { "ClubId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClubMemberships_ClubRoleDefinitions_ClubRoleDefinitionId",
                table: "ClubMemberships",
                column: "ClubRoleDefinitionId",
                principalTable: "ClubRoleDefinitions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // O-20: mevcut kulüplere varsayılan beşli. Yeni kulüpler bunu Business'ta alıyor
            // (ClubManager.CreateAsync / ClubApplicationManager.DecideAsync); bu satır yalnızca
            // migration anındaki kulüpler için. Bu adım olmadan mevcut kulüplerin "Roller"
            // sekmesi boş açılır ve kimse sebebini anlamaz.
            // Sayısal değerler Entities.Enums.ClubRole ile birebir: Member = 0, Officer = 1, President = 2.
            migrationBuilder.Sql("""
                INSERT INTO [ClubRoleDefinitions] ([ClubId], [Name], [ClubRole], [DisplayOrder])
                SELECT c.[Id], v.[Name], v.[ClubRole], v.[DisplayOrder]
                FROM [Clubs] c
                CROSS JOIN (VALUES
                    (N'Başkan', 2, 1),
                    (N'Başkan Yardımcısı', 1, 2),
                    (N'Sayman', 1, 3),
                    (N'Sekreter', 1, 4),
                    (N'Üye', 0, 5)
                ) AS v([Name], [ClubRole], [DisplayOrder]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubMemberships_ClubRoleDefinitions_ClubRoleDefinitionId",
                table: "ClubMemberships");

            migrationBuilder.DropTable(
                name: "ClubRoleDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_ClubMemberships_ClubRoleDefinitionId",
                table: "ClubMemberships");

            migrationBuilder.DropColumn(
                name: "ClubRoleDefinitionId",
                table: "ClubMemberships");
        }
    }
}
