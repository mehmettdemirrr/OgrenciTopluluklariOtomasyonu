using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class _20260827_Faz35_KulupYetkiMatrisi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Capabilities",
                table: "ClubRoleDefinitions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Capabilities",
                table: "ClubMemberships",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // A-68: mevcut satırların kapasitesi ClubRole'den türetilir — Faz 34 öncesi davranış birebir korunur.
            // Sayılar ClubCapability ile birebir: MembersView=1, MembersManage=2, EventsManage=4,
            // EventParticipantsView=8, AnnouncementsManage=16, ReportsView=32.
            // Officer = 1+4+8+16+32 = 61, President = 61+2 = 63, Member = 0.
            // ClubRole: Member=0, Officer=1, President=2.
            // Bu adım atlanırsa mevcut HER ÜYE kapasitesiz kalır ve hiçbir şey yapamaz.
            migrationBuilder.Sql("""
                UPDATE [ClubMemberships] SET [Capabilities] = CASE [ClubRole]
                    WHEN 2 THEN 63
                    WHEN 1 THEN 61
                    ELSE 0 END;

                UPDATE [ClubRoleDefinitions] SET [Capabilities] = CASE [ClubRole]
                    WHEN 2 THEN 63
                    WHEN 1 THEN 61
                    ELSE 0 END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Capabilities",
                table: "ClubRoleDefinitions");

            migrationBuilder.DropColumn(
                name: "Capabilities",
                table: "ClubMemberships");
        }
    }
}
