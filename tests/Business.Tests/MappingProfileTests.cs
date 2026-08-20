using AutoMapper;
using Business.Mappings;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · Y-32: AutoMapper profilleri için zorunlu doğrulama testi.</summary>
public class MappingProfileTests
{
    [Fact(DisplayName = "ClubMappingProfile geçerlidir")]
    public void ClubMappingProfile_IsValid()
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<ClubMappingProfile>(), NullLoggerFactory.Instance);

        configuration.AssertConfigurationIsValid();
    }
}
