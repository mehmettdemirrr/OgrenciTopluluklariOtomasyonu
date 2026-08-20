namespace Business.DTOs.Reports;

/// <summary>ReportRequest.ParametersJson'ın şekli — talep anında dondurulur, üretim/indirme anında aynen okunur.</summary>
public sealed record ReportParameters(int? ClubId, int? EventId, int? AcademicTermId);
