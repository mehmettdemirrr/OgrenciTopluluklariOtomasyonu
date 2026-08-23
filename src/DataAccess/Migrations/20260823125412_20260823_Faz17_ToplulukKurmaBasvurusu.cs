using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class _20260823_Faz17_ToplulukKurmaBasvurusu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClubApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    AcademicTermId = table.Column<int>(type: "int", nullable: false),
                    ProposedName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Justification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProposedAdvisorId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AppliedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedClubId = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClubApplications_AcademicStaff_ProposedAdvisorId",
                        column: x => x.ProposedAdvisorId,
                        principalTable: "AcademicStaff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClubApplications_AcademicTerms_AcademicTermId",
                        column: x => x.AcademicTermId,
                        principalTable: "AcademicTerms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClubApplications_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClubApplications_Clubs_CreatedClubId",
                        column: x => x.CreatedClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClubApplications_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClubApplications_AcademicTermId",
                table: "ClubApplications",
                column: "AcademicTermId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubApplications_CreatedClubId",
                table: "ClubApplications",
                column: "CreatedClubId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubApplications_ProposedAdvisorId",
                table: "ClubApplications",
                column: "ProposedAdvisorId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubApplications_ReviewedByUserId",
                table: "ClubApplications",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubApplications_StudentId_AcademicTermId",
                table: "ClubApplications",
                columns: new[] { "StudentId", "AcademicTermId" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [Status] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClubApplications");
        }
    }
}
