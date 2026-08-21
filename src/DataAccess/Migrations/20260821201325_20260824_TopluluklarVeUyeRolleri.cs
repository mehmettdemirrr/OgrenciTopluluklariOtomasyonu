using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class _20260824_TopluluklarVeUyeRolleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ClubMemberships_ClubId_AcademicTermId_President",
                table: "ClubMemberships",
                columns: new[] { "ClubId", "AcademicTermId" },
                unique: true,
                filter: "[ClubRole] = 2 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClubMemberships_ClubId_AcademicTermId_President",
                table: "ClubMemberships");
        }
    }
}
