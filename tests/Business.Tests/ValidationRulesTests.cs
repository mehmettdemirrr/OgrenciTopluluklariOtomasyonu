using Business.DTOs.Events;
using Business.DTOs.Memberships;
using Business.DTOs.Reports;
using Business.ValidationRules;
using Entities.Enums;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · A-20: her doğrulama kuralı için bir kabul + bir ret.</summary>
public class ValidationRulesTests
{
    [Fact(DisplayName = "ApplyForMembership: ClubId > 0 geçerlidir")]
    public void ApplyForMembership_PositiveClubId_IsValid()
    {
        var result = new ApplyForMembershipRequestValidator().Validate(new ApplyForMembershipRequestDto { ClubId = 1 });

        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "ApplyForMembership: ClubId <= 0 geçersizdir")]
    public void ApplyForMembership_NonPositiveClubId_IsInvalid()
    {
        var result = new ApplyForMembershipRequestValidator().Validate(new ApplyForMembershipRequestDto { ClubId = 0 });

        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "ReviewMembershipApplication: Approved/Rejected geçerlidir")]
    public void ReviewMembershipApplication_ApprovedOrRejected_IsValid()
    {
        var validator = new ReviewMembershipApplicationRequestValidator();

        Assert.True(validator.Validate(new ReviewMembershipApplicationRequestDto { Status = ApplicationStatus.Approved }).IsValid);
        Assert.True(validator.Validate(new ReviewMembershipApplicationRequestDto { Status = ApplicationStatus.Rejected }).IsValid);
    }

    [Fact(DisplayName = "ReviewMembershipApplication: Pending geçersizdir")]
    public void ReviewMembershipApplication_Pending_IsInvalid()
    {
        var result = new ReviewMembershipApplicationRequestValidator().Validate(new ReviewMembershipApplicationRequestDto { Status = ApplicationStatus.Pending });

        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "CreateReportRequest: ClubMembers için pozitif ClubId geçerlidir")]
    public void CreateReportRequest_ClubMembersWithPositiveClubId_IsValid()
    {
        var result = new CreateReportRequestValidator().Validate(new CreateReportRequestDto { ReportType = ReportType.ClubMembers, ClubId = 1 });

        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "CreateReportRequest: ClubMembers için ClubId eksikse geçersizdir")]
    public void CreateReportRequest_ClubMembersWithoutClubId_IsInvalid()
    {
        var result = new CreateReportRequestValidator().Validate(new CreateReportRequestDto { ReportType = ReportType.ClubMembers, ClubId = null });

        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "CreateReportRequest: EventParticipants için pozitif EventId geçerlidir")]
    public void CreateReportRequest_EventParticipantsWithPositiveEventId_IsValid()
    {
        var result = new CreateReportRequestValidator().Validate(new CreateReportRequestDto { ReportType = ReportType.EventParticipants, EventId = 1 });

        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "CreateReportRequest: EventParticipants için EventId eksikse geçersizdir")]
    public void CreateReportRequest_EventParticipantsWithoutEventId_IsInvalid()
    {
        var result = new CreateReportRequestValidator().Validate(new CreateReportRequestDto { ReportType = ReportType.EventParticipants, EventId = null });

        Assert.False(result.IsValid);
    }

    [Fact(DisplayName = "Y-72: tanımsız EventAudience değeri biçimsel doğrulamada reddedilir")]
    public void CreateEventRequestValidator_UndefinedAudience_IsInvalid()
    {
        var validator = new CreateEventRequestValidator();
        var request = new CreateEventRequestDto
        {
            Title = "Etkinlik",
            StartDateUtc = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc),
            Audience = (EventAudience)99,
        };

        var result = validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateEventRequestDto.Audience));
    }

    [Fact(DisplayName = "Y-72: tanımlı EventAudience değeri biçimsel doğrulamadan geçer")]
    public void CreateEventRequestValidator_DefinedAudience_IsValid()
    {
        var validator = new CreateEventRequestValidator();
        var request = new CreateEventRequestDto
        {
            Title = "Etkinlik",
            StartDateUtc = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc),
            Audience = EventAudience.ClubMembers,
        };

        var result = validator.Validate(request);

        Assert.True(result.IsValid);
    }
}
