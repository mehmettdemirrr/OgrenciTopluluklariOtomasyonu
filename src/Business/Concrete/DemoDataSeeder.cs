using Business.Abstract;
using Core.DataAccess;
using Core.Utilities.Time;
using DataAccess.Repositories;
using DataAccess.Seed;
using Entities;
using Entities.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Business.Concrete;

/// <summary>
/// docs/MIMARI.md · Y-68 (K-34, A-58): demo veri üretimi.
///
/// Üç koruma katmanı:
/// <list type="number">
///   <item><c>Seed:Demo</c> açık değilse hiçbir şey yapmaz — üretimde varsayılan budur.</item>
///   <item>Parola yalnızca <c>Seed:DemoPassword</c>'dan gelir (Y-20); yoksa atlanır.</item>
///   <item>Ürettiği her satırı <see cref="DemoSeedRecord"/> ile künyeler — sıfırlama yalnızca
///         künyeli satırlara dokunabilir, ad kalıbına bakan toplu silme yoktur.</item>
/// </list>
///
/// İdempotanlık: künye tablosunda kayıt varsa demo veri zaten üretilmiştir ve seeder çıkar.
/// <c>Seed:ResetDemo=true</c> önce künyeli veriyi siler, sonra yeniden üretir.
/// </summary>
public sealed class DemoDataSeeder(
    UserManager<ApplicationUser> userManager,
    IDemoDataDal demoDataDal,
    IEntityRepository<DemoSeedRecord> demoSeedRecordRepository,
    IEntityRepository<AcademicTerm> academicTermRepository,
    IEntityRepository<Department> departmentRepository,
    IEntityRepository<AcademicStaff> academicStaffRepository,
    IEntityRepository<Student> studentRepository,
    IEntityRepository<Club> clubRepository,
    IEntityRepository<ClubMembership> clubMembershipRepository,
    IEntityRepository<MembershipApplication> membershipApplicationRepository,
    IEntityRepository<ClubApplication> clubApplicationRepository,
    IEntityRepository<Event> eventRepository,
    IEntityRepository<EventParticipation> eventParticipationRepository,
    IEntityRepository<Announcement> announcementRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    IConfiguration configuration) : IDemoDataSeeder
{
    /// <summary>Her kulüpteki üye listesi ve rol dağılımı — başkan tekilliği (A-39) elle garanti edilir.</summary>
    private static readonly (int[] StudentIndexes, int PresidentIndex, int OfficerIndex)[] Memberships =
    [
        ([0, 1, 2, 3, 4, 5], 0, 1),
        ([1, 3, 6, 7, 8], 3, 6),
        ([9, 10, 11, 19, 20, 21], 20, 21),
        ([5, 6, 12, 13, 14], 6, 12),
        ([8, 15, 16, 17, 18], 16, 17),
        ([10, 11, 22, 23], 11, 22),
        ([15, 16, 23, 24], 24, 23),
        ([2, 13], 13, -1),
    ];

    public async Task<DemoSeedOutcome> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!IsEnabled("Seed:Demo"))
        {
            return DemoSeedOutcome.Disabled;
        }

        var password = configuration["Seed:DemoPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            return DemoSeedOutcome.PasswordMissing;
        }

        var reset = IsEnabled("Seed:ResetDemo");
        var alreadySeeded = await demoSeedRecordRepository
            .GetAsync(_ => true, cancellationToken).ConfigureAwait(false) is not null;

        if (alreadySeeded && !reset)
        {
            return DemoSeedOutcome.AlreadyPresent;
        }

        if (alreadySeeded)
        {
            await PurgeAsync(cancellationToken).ConfigureAwait(false);
        }

        await BuildAsync(password, cancellationToken).ConfigureAwait(false);

        return alreadySeeded ? DemoSeedOutcome.Recreated : DemoSeedOutcome.Created;
    }

    /// <summary>
    /// Domain satırlarını DAL siler (IgnoreQueryFilters + hard delete gerekir); kullanıcıları
    /// UserManager siler, çünkü Identity'nin rol/claim/token tabloları onun sorumluluğunda.
    ///
    /// <b>Tek transaction:</b> demo olmayan gerçek bir kayıt demo bir satıra bağlanmışsa
    /// (ör. yönetici gerçek bir kulübe demo danışman atadıysa) FK kısıtı silmeyi reddeder.
    /// O durumda kısmen boşaltılmış bir veritabanı bırakmak, hata vermekten çok daha kötüdür —
    /// bu yüzden ya hepsi gider ya hiçbiri.
    /// </summary>
    private async Task PurgeAsync(CancellationToken cancellationToken)
    {
        var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            try
            {
                var userIds = await demoDataDal.PurgeAsync(cancellationToken).ConfigureAwait(false);

                foreach (var userId in userIds)
                {
                    if (await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false) is { } user)
                    {
                        await userManager.DeleteAsync(user).ConfigureAwait(false);
                    }
                }

                var leftoverRecords = await demoSeedRecordRepository
                    .GetListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                foreach (var record in leftoverRecords)
                {
                    demoSeedRecordRepository.Delete(record);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }

    private async Task BuildAsync(string password, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var term = await academicTermRepository.GetAsync(t => t.IsCurrent, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Demo veri üretilemedi: güncel akademik dönem yok.");

        // Bölümler ada göre çözülür (bkz. DemoSeedData.DemoAdvisor). Aynı ad birden çok fakültede
        // olabildiği için en küçük Id kazanır — böylece sonuç veritabanından bağımsız olarak
        // deterministiktir. Ad bulunamazsa Id 1'e düşülür: o satırın varlığını migration garanti eder.
        var departmentIdsByName = (await departmentRepository
                .GetListAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            .GroupBy(d => d.Name)
            .ToDictionary(g => g.Key, g => g.Min(d => d.Id));

        var advisorIds = await SeedAdvisorsAsync(password, departmentIdsByName, cancellationToken).ConfigureAwait(false);
        var studentIds = await SeedStudentsAsync(password, departmentIdsByName, cancellationToken).ConfigureAwait(false);
        var clubIds = await SeedClubsAsync(advisorIds, now, cancellationToken).ConfigureAwait(false);

        await SeedMembershipsAsync(clubIds, studentIds, term.Id, now, cancellationToken).ConfigureAwait(false);
        await SeedApplicationsAsync(clubIds, studentIds, advisorIds, term.Id, now, cancellationToken).ConfigureAwait(false);

        var eventIds = await SeedEventsAsync(clubIds, now, cancellationToken).ConfigureAwait(false);
        await SeedParticipationsAsync(eventIds, studentIds, now, cancellationToken).ConfigureAwait(false);
        await SeedAnnouncementsAsync(clubIds, now, cancellationToken).ConfigureAwait(false);
    }

    private async Task<int[]> SeedAdvisorsAsync(
        string password, Dictionary<string, int> departmentIdsByName, CancellationToken cancellationToken)
    {
        var catalog = DemoSeedData.Advisors();
        var ids = new int[catalog.Length];

        for (var i = 0; i < catalog.Length; i++)
        {
            var advisor = catalog[i];
            var user = await CreateUserAsync(
                advisor.FirstName, advisor.LastName, $"danisman{i + 1}", password,
                IdentitySeedData.AdvisorRoleName, cancellationToken).ConfigureAwait(false);

            var staff = new AcademicStaff
            {
                ApplicationUserId = user.Id,
                Title = advisor.Title,
                DepartmentId = ResolveDepartment(departmentIdsByName, advisor.DepartmentName),
            };
            await academicStaffRepository.AddAsync(staff, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await TagAsync(DemoEntityTypes.AcademicStaff, staff.Id, cancellationToken).ConfigureAwait(false);
            ids[i] = staff.Id;
        }

        return ids;
    }

    private async Task<int[]> SeedStudentsAsync(
        string password, Dictionary<string, int> departmentIdsByName, CancellationToken cancellationToken)
    {
        var catalog = DemoSeedData.Students();
        var ids = new int[catalog.Length];

        for (var i = 0; i < catalog.Length; i++)
        {
            var demo = catalog[i];
            var user = await CreateUserAsync(
                demo.FirstName, demo.LastName, $"ogrenci{i + 1}", password,
                IdentitySeedData.MemberRoleName, cancellationToken).ConfigureAwait(false);

            var student = new Student
            {
                ApplicationUserId = user.Id,
                StudentNumber = demo.StudentNumber,
                DepartmentId = ResolveDepartment(departmentIdsByName, demo.DepartmentName),
                EnrollmentYear = demo.EnrollmentYear,
            };
            await studentRepository.AddAsync(student, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await TagAsync(DemoEntityTypes.Student, student.Id, cancellationToken).ConfigureAwait(false);
            ids[i] = student.Id;
        }

        return ids;
    }

    private static int ResolveDepartment(Dictionary<string, int> departmentIdsByName, string name) =>
        departmentIdsByName.GetValueOrDefault(name, DomainSeedData.DepartmentId);

    private async Task<ApplicationUser> CreateUserAsync(
        string firstName, string lastName, string localPart, string password, string roleName, CancellationToken cancellationToken)
    {
        var email = localPart + DemoSeedData.EmailDomain;
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
        };

        var result = await userManager.CreateAsync(user, password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Demo kullanıcısı oluşturulamadı ({email}): " + string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(user, roleName).ConfigureAwait(false);
        await TagAsync(DemoEntityTypes.ApplicationUser, user.Id, cancellationToken).ConfigureAwait(false);

        return user;
    }

    private async Task<int[]> SeedClubsAsync(int[] advisorIds, DateTime now, CancellationToken cancellationToken)
    {
        var catalog = DemoSeedData.Clubs();
        var ids = new int[catalog.Length];

        for (var i = 0; i < catalog.Length; i++)
        {
            var demo = catalog[i];
            var club = new Club
            {
                Name = demo.Name,
                Description = demo.Description,
                AdvisorId = advisorIds[demo.AdvisorIndex],
                IsActive = demo.IsActive,
                CreatedAtUtc = now.AddDays(-120 + (i * 7)),
            };
            await clubRepository.AddAsync(club, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await TagAsync(DemoEntityTypes.Club, club.Id, cancellationToken).ConfigureAwait(false);
            ids[i] = club.Id;
        }

        return ids;
    }

    private async Task SeedMembershipsAsync(
        int[] clubIds, int[] studentIds, int termId, DateTime now, CancellationToken cancellationToken)
    {
        for (var clubIndex = 0; clubIndex < Memberships.Length; clubIndex++)
        {
            var (studentIndexes, presidentIndex, officerIndex) = Memberships[clubIndex];

            foreach (var studentIndex in studentIndexes)
            {
                var role = studentIndex == presidentIndex
                    ? ClubRole.President
                    : studentIndex == officerIndex ? ClubRole.Officer : ClubRole.Member;

                var membership = new ClubMembership
                {
                    ClubId = clubIds[clubIndex],
                    StudentId = studentIds[studentIndex],
                    AcademicTermId = termId,
                    ClubRole = role,
                    JoinedAtUtc = now.AddDays(-90 + studentIndex),
                };
                await clubMembershipRepository.AddAsync(membership, cancellationToken).ConfigureAwait(false);
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                await TagAsync(DemoEntityTypes.ClubMembership, membership.Id, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// İnceleme ekranlarının boş kalmaması için bekleyen/karara bağlanmış başvurular.
    /// Üyelik başvuruları <b>üye olunmayan</b> kulüplere yazılır — (ClubId, StudentId, AcademicTermId)
    /// benzersizliği üyelikte, başvuruda ayrıdır ama gerçekçi olmayan durum üretmemek için kaçınılır.
    /// </summary>
    private async Task SeedApplicationsAsync(
        int[] clubIds, int[] studentIds, int[] advisorIds, int termId, DateTime now, CancellationToken cancellationToken)
    {
        (int ClubIndex, int StudentIndex, ApplicationStatus Status)[] applications =
        [
            (0, 9, ApplicationStatus.Pending),
            (0, 24, ApplicationStatus.Pending),
            (1, 14, ApplicationStatus.Pending),
            (2, 4, ApplicationStatus.Pending),
            (3, 22, ApplicationStatus.Pending),
            (4, 0, ApplicationStatus.Pending),
            (5, 19, ApplicationStatus.Approved),
            (6, 7, ApplicationStatus.Approved),
            (1, 12, ApplicationStatus.Rejected),
            (4, 2, ApplicationStatus.Rejected),
        ];

        foreach (var (clubIndex, studentIndex, status) in applications)
        {
            var appliedAt = now.AddDays(-20 + studentIndex);
            var application = new MembershipApplication
            {
                ClubId = clubIds[clubIndex],
                StudentId = studentIds[studentIndex],
                AcademicTermId = termId,
                Status = status,
                AppliedAtUtc = appliedAt,
                ReviewedAtUtc = status == ApplicationStatus.Pending ? null : appliedAt.AddDays(2),
            };
            await membershipApplicationRepository.AddAsync(application, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await TagAsync(DemoEntityTypes.MembershipApplication, application.Id, cancellationToken).ConfigureAwait(false);
        }

        (int StudentIndex, string Name, string Justification, int AdvisorIndex, ApplicationStatus Status)[] clubApplications =
        [
            (3, "Sürdürülebilir Enerji Topluluğu", "Yenilenebilir enerji projelerinde çalışmak isteyen öğrencileri bir araya getirmek istiyoruz.", 0, ApplicationStatus.Pending),
            (17, "Satranç ve Zekâ Oyunları Kulübü", "Kampüste düzenli turnuvalar ve eğitim seansları düzenlemeyi planlıyoruz.", 3, ApplicationStatus.Pending),
            (21, "Münazara Topluluğu", "Öğrencilerin sunum ve tartışma becerilerini geliştirmeyi hedefliyoruz.", 2, ApplicationStatus.Rejected),
        ];

        foreach (var (studentIndex, name, justification, advisorIndex, status) in clubApplications)
        {
            var appliedAt = now.AddDays(-15 + studentIndex);
            var application = new ClubApplication
            {
                StudentId = studentIds[studentIndex],
                AcademicTermId = termId,
                ProposedName = name,
                Description = justification,
                Justification = justification,
                ProposedAdvisorId = advisorIds[advisorIndex],
                Status = status,
                AppliedAtUtc = appliedAt,
                ReviewedAtUtc = status == ApplicationStatus.Pending ? null : appliedAt.AddDays(3),
                ReviewNote = status == ApplicationStatus.Rejected ? "Benzer amaçlı bir topluluk hâlihazırda faaliyette." : null,
            };
            await clubApplicationRepository.AddAsync(application, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await TagAsync(DemoEntityTypes.ClubApplication, application.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<int[]> SeedEventsAsync(int[] clubIds, DateTime now, CancellationToken cancellationToken)
    {
        var catalog = DemoSeedData.Events();
        var ids = new int[catalog.Length];

        for (var i = 0; i < catalog.Length; i++)
        {
            var demo = catalog[i];
            var start = now.Date.AddDays(demo.StartDayOffset).AddHours(10);

            var @event = new Event
            {
                ClubId = clubIds[demo.ClubIndex],
                Title = demo.Title,
                Description = demo.Title + " — topluluk üyelerine ve ilgilenen tüm öğrencilere açıktır.",
                Location = demo.Location,
                StartDateUtc = start,
                EndDateUtc = start.AddHours(demo.DurationHours),
                Capacity = demo.Capacity,
                Status = demo.Status,
                // A-49: iptal gerekçesi zorunlu bilgidir — iptal edilmiş demo etkinliği gerekçesiz bırakmak
                // iptal rozetinin gerçek hâlini göstermezdi.
                CancellationReason = demo.Status == EventStatus.Cancelled
                    ? "Olumsuz hava koşulları nedeniyle iptal edilmiştir."
                    : null,
                CreatedAtUtc = now.AddDays(-45 + i),
            };
            await eventRepository.AddAsync(@event, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await TagAsync(DemoEntityTypes.Event, @event.Id, cancellationToken).ConfigureAwait(false);
            ids[i] = @event.Id;
        }

        return ids;
    }

    /// <summary>
    /// Yayındaki her etkinliğe katılımcı yazar. Kontenjanı 3 olan "Veri Maratonu" tam olarak
    /// dolar — kontenjan dolu rozetinin gerçek bir kaydı olsun diye.
    /// </summary>
    private async Task SeedParticipationsAsync(
        int[] eventIds, int[] studentIds, DateTime now, CancellationToken cancellationToken)
    {
        var catalog = DemoSeedData.Events();

        for (var i = 0; i < catalog.Length; i++)
        {
            var demo = catalog[i];
            if (demo.Status != EventStatus.Published)
            {
                continue;
            }

            var participantCount = demo.Capacity is { } capacity
                ? Math.Min(capacity, 6)
                : 5;

            for (var slot = 0; slot < participantCount; slot++)
            {
                var studentIndex = ((i * 3) + slot) % studentIds.Length;
                var participation = new EventParticipation
                {
                    EventId = eventIds[i],
                    StudentId = studentIds[studentIndex],
                    RegisteredAtUtc = now.AddDays(-10).AddHours(slot),
                };
                await eventParticipationRepository.AddAsync(participation, cancellationToken).ConfigureAwait(false);
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                await TagAsync(DemoEntityTypes.EventParticipation, participation.Id, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task SeedAnnouncementsAsync(int[] clubIds, DateTime now, CancellationToken cancellationToken)
    {
        foreach (var demo in DemoSeedData.Announcements())
        {
            var announcement = new Announcement
            {
                ClubId = demo.ClubIndex < 0 ? null : clubIds[demo.ClubIndex],
                Title = demo.Title,
                Content = demo.Content,
                Visibility = demo.Visibility,
                PublishedAtUtc = now.AddDays(demo.PublishedDayOffset),
            };
            await announcementRepository.AddAsync(announcement, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await TagAsync(DemoEntityTypes.Announcement, announcement.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Y-68: anahtar <b>açıkça</b> "true" olmadıkça kapalıdır. Business yalnızca
    /// Configuration.Abstractions'a bağlı (Binder yok), bu yüzden dönüşüm elle yapılır —
    /// ve tanınmayan bir değer ("1", "yes", boş) kazara açık sayılmaz.
    /// </summary>
    private bool IsEnabled(string key) =>
        bool.TryParse(configuration[key], out var enabled) && enabled;

    private async Task TagAsync(string entityType, int entityId, CancellationToken cancellationToken)
    {
        await demoSeedRecordRepository.AddAsync(
            new DemoSeedRecord { EntityType = entityType, EntityId = entityId, CreatedAtUtc = clock.UtcNow },
            cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
