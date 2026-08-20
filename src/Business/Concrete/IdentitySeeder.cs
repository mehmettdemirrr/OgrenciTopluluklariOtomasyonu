using Business.Abstract;
using Core.DataAccess;
using Core.Utilities.Time;
using DataAccess.Seed;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Business.Concrete;

/// <summary>
/// docs/MIMARI.md · A-27/Y-19/Y-20: admin ve Faz 5'in dikey dilimini denenebilir kılan demo
/// danışman/öğrenci/kulüp kullanıcılarını create-if-missing olarak seed eder (parola hash'i
/// UserManager gerektirdiği için HasData ile üretilemez). Seed bilgisi config'ten gelir, boşsa
/// ilgili adım sessizce atlanır — appsettings*.json'a asla parola yazılmaz (Y-20).
/// </summary>
public sealed class IdentitySeeder(
    UserManager<ApplicationUser> userManager,
    IEntityRepository<AcademicStaff> academicStaffRepository,
    IEntityRepository<Student> studentRepository,
    IEntityRepository<Club> clubRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    IConfiguration configuration) : IIdentitySeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedAdminAsync().ConfigureAwait(false);
        await SeedDemoAdvisorAndClubAsync(cancellationToken).ConfigureAwait(false);
        await SeedDemoStudentAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedAdminAsync()
    {
        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (await userManager.FindByEmailAsync(email).ConfigureAwait(false) is not null)
        {
            return;
        }

        var admin = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var createResult = await userManager.CreateAsync(admin, password).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Admin seed kullanıcısı oluşturulamadı: " + string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(admin, IdentitySeedData.AdminRoleName).ConfigureAwait(false);
    }

    private async Task SeedDemoAdvisorAndClubAsync(CancellationToken cancellationToken)
    {
        var email = configuration["Seed:DemoAdvisorEmail"];
        var password = configuration["Seed:DemoAdvisorPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (await userManager.FindByEmailAsync(email).ConfigureAwait(false) is not null)
        {
            return;
        }

        var advisorUser = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var createResult = await userManager.CreateAsync(advisorUser, password).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Demo danışman seed kullanıcısı oluşturulamadı: " + string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(advisorUser, IdentitySeedData.AdvisorRoleName).ConfigureAwait(false);

        var academicStaff = new AcademicStaff
        {
            ApplicationUserId = advisorUser.Id,
            Title = "Dr. Öğr. Üyesi",
            DepartmentId = DomainSeedData.DepartmentId,
        };
        await academicStaffRepository.AddAsync(academicStaff, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var club = new Club
        {
            Name = "Yazılım Geliştirme Topluluğu",
            Description = "Öğrencilerin yazılım projelerinde birlikte çalıştığı topluluk.",
            AdvisorId = academicStaff.Id,
            IsActive = true,
            CreatedAtUtc = clock.UtcNow,
        };
        await clubRepository.AddAsync(club, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedDemoStudentAsync(CancellationToken cancellationToken)
    {
        var email = configuration["Seed:DemoStudentEmail"];
        var password = configuration["Seed:DemoStudentPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (await userManager.FindByEmailAsync(email).ConfigureAwait(false) is not null)
        {
            return;
        }

        var studentUser = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var createResult = await userManager.CreateAsync(studentUser, password).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Demo öğrenci seed kullanıcısı oluşturulamadı: " + string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(studentUser, IdentitySeedData.MemberRoleName).ConfigureAwait(false);

        var student = new Student
        {
            ApplicationUserId = studentUser.Id,
            StudentNumber = "20260001",
            DepartmentId = DomainSeedData.DepartmentId,
            EnrollmentYear = 2026,
        };
        await studentRepository.AddAsync(student, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
