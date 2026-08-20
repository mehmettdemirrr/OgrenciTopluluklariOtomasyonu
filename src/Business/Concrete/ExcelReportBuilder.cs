using Business.Abstract;
using ClosedXML.Excel;
using Entities.Dtos.Reports;

namespace Business.Concrete;

/// <summary>docs/MIMARI.md · K-06: ClosedXML (MIT) — EPPlus lisans koşulları nedeniyle kullanılmaz.</summary>
public sealed class ExcelReportBuilder : IExcelReportBuilder
{
    public byte[] BuildClubMemberWorkbook(IReadOnlyList<ClubMemberExportRowDto> rows, string clubName, string termName)
    {
        using var workbook = new XLWorkbook();
        workbook.Properties.Title = $"{clubName} - {termName}";
        var sheet = workbook.Worksheets.Add("Üyeler");

        sheet.Cell(1, 1).Value = "Öğrenci No";
        sheet.Cell(1, 2).Value = "E-posta";
        sheet.Cell(1, 3).Value = "Bölüm";
        sheet.Cell(1, 4).Value = "Rol";
        sheet.Cell(1, 5).Value = "Katılım Tarihi";
        sheet.Row(1).Style.Font.Bold = true;

        var rowIndex = 2;
        foreach (var row in rows)
        {
            sheet.Cell(rowIndex, 1).Value = row.StudentNumber;
            sheet.Cell(rowIndex, 2).Value = row.Email;
            sheet.Cell(rowIndex, 3).Value = row.DepartmentName;
            sheet.Cell(rowIndex, 4).Value = row.ClubRole.ToString();
            sheet.Cell(rowIndex, 5).Value = row.JoinedAtUtc.ToString("o");
            rowIndex++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] BuildEventParticipationWorkbook(IReadOnlyList<EventParticipationExportRowDto> rows, string eventTitle)
    {
        using var workbook = new XLWorkbook();
        workbook.Properties.Title = eventTitle;
        var sheet = workbook.Worksheets.Add("Katılımcılar");

        sheet.Cell(1, 1).Value = "Öğrenci No";
        sheet.Cell(1, 2).Value = "E-posta";
        sheet.Cell(1, 3).Value = "Etkinlik";
        sheet.Cell(1, 4).Value = "Kayıt Tarihi";
        sheet.Row(1).Style.Font.Bold = true;

        var rowIndex = 2;
        foreach (var row in rows)
        {
            sheet.Cell(rowIndex, 1).Value = row.StudentNumber;
            sheet.Cell(rowIndex, 2).Value = row.Email;
            sheet.Cell(rowIndex, 3).Value = row.EventTitle;
            sheet.Cell(rowIndex, 4).Value = row.RegisteredAtUtc.ToString("o");
            rowIndex++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
