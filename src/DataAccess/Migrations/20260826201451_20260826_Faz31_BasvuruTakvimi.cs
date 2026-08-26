using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class _20260826_Faz31_BasvuruTakvimi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClubApplicationEndUtc",
                table: "AcademicTerms",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClubApplicationOverride",
                table: "AcademicTerms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClubApplicationStartUtc",
                table: "AcademicTerms",
                type: "datetime2",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AcademicTerms",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ClubApplicationEndUtc", "ClubApplicationOverride", "ClubApplicationStartUtc" },
                values: new object[] { null, 1, null });

            // docs/MIMARI.md · A-66: yukarıdaki UpdateData yalnızca seed dönemini (Id = 1) kapsar.
            // SetCurrentAsync ile güncel dönem başka bir satıra taşınmış kurulumlarda o satır
            // FollowSchedule + tarihsiz kalırdı, yani fail-closed kuralı gereği KAPALI — dağıtım
            // anında bugüne kadar hep açık olan başvuru akışı sessizce dururdu.
            //
            // Bu satırın YERİ önemli: UpdateData'dan SONRA olmalı. Önce yazılsaydı ve güncel dönem
            // Id = 1 ise, UpdateData onu ezerdi.
            migrationBuilder.Sql("UPDATE [AcademicTerms] SET [ClubApplicationOverride] = 1 WHERE [IsCurrent] = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClubApplicationEndUtc",
                table: "AcademicTerms");

            migrationBuilder.DropColumn(
                name: "ClubApplicationOverride",
                table: "AcademicTerms");

            migrationBuilder.DropColumn(
                name: "ClubApplicationStartUtc",
                table: "AcademicTerms");
        }
    }
}
