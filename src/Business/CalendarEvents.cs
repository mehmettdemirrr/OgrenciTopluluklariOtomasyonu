using Business.Constants;
using Business.DTOs.Public;
using Entities;
using Entities.Enums;

namespace Business;

/// <summary>Anasayfa takvimi: tarih aralığı ve kilitli/açık DTO dönüşümü tek yerde.</summary>
internal static class CalendarEvents
{
    public const int MaxRangeDays = 62;

    public static bool TryResolveRange(
        DateTime? fromUtc, DateTime? toUtc, DateTime nowUtc, out DateTime from, out DateTime to, out string? error)
    {
        from = default;
        to = default;
        error = null;

        if (fromUtc is null && toUtc is null)
        {
            from = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            to = from.AddMonths(1);
            return true;
        }

        if (fromUtc is null || toUtc is null || toUtc.Value <= fromUtc.Value || (toUtc.Value - fromUtc.Value).TotalDays > MaxRangeDays)
        {
            error = Messages.InvalidCalendarRange;
            return false;
        }

        from = DateTime.SpecifyKind(fromUtc.Value, DateTimeKind.Utc);
        to = DateTime.SpecifyKind(toUtc.Value, DateTimeKind.Utc);
        return true;
    }

    public static PublicCalendarEventDto ToOpen(Event e, string clubName) => new()
    {
        Locked = false,
        Id = e.Id,
        ClubId = e.ClubId,
        ClubName = clubName,
        Title = e.Title,
        Location = e.Location,
        StartDateUtc = e.StartDateUtc,
        EndDateUtc = e.EndDateUtc,
        PosterFileId = e.PosterFileId,
    };

    public static PublicCalendarEventDto ToLocked(Event e) => new()
    {
        Locked = true,
        StartDateUtc = e.StartDateUtc,
        EndDateUtc = e.EndDateUtc,
    };

    public static bool IsMembersOnly(Event e) => e.Audience == EventAudience.ClubMembers;
}
