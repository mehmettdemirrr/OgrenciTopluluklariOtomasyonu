using System.Text.Encodings.Web;
using System.Text.Json;
using Core.Entities;
using Core.Utilities.Security;
using Core.Utilities.Time;
using Entities;
using Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DataAccess.Interceptors;

/// <summary>
/// docs/MIMARI.md · K-12/A-33/Y-44: her yazma işlemi tek yerden, otomatik olarak denetlenir.
/// Y-26: parola/token/hash alanları asla audit'e yazılmaz. Y-44: soft delete de "silme" sayılır.
///
/// Insert edilen kayıtların PK'sı SaveChanges tamamlanana kadar bilinmediği için (identity kolon),
/// değişiklikler SavingChangesAsync'te yakalanır ama AuditLog satırları SavedChangesAsync'te
/// (asıl kayıt commit olduktan, PK'lar dolduktan sonra) ikinci bir SaveChangesAsync ile yazılır.
/// AuditLog'un kendisi asla audit edilmez (BuildPendingEntries'te hariç tutulur) — bu, ikinci
/// SaveChangesAsync çağrısının sonsuz döngüye girmesini de engeller.
/// </summary>
public sealed class AuditSaveChangesInterceptor(ICurrentUser currentUser, IClock clock) : SaveChangesInterceptor
{
    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "TokenHash",
    };

    // Türkçe karakterlerin \uXXXX olarak escape edilmemesi için — audit kaydı okunabilir kalsın.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
    };

    private List<PendingAuditEntry> _pending = [];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is { } context)
        {
            _pending = BuildPendingEntries(context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
        {
            _pending = BuildPendingEntries(context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (_pending.Count > 0 && eventData.Context is { } context)
        {
            var pending = _pending;
            _pending = [];

            context.Set<AuditLog>().AddRange(pending.Select(p => p.ToAuditLog(currentUser.UserId, clock.UtcNow)));
            context.SaveChanges();
        }

        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (_pending.Count > 0 && eventData.Context is { } context)
        {
            var pending = _pending;
            _pending = [];

            context.Set<AuditLog>().AddRange(pending.Select(p => p.ToAuditLog(currentUser.UserId, clock.UtcNow)));
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    private static List<PendingAuditEntry> BuildPendingEntries(DbContext context)
    {
        var entries = new List<PendingAuditEntry>();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // AuditLog kendisi audit edilmez — hem K-12/Y-44'ün gereği hem de SavedChanges'teki
            // ikinci SaveChanges çağrısının sonsuz döngüye girmemesi için zorunlu.
            if (entry.Entity is AuditLog)
            {
                continue;
            }

            var action = ResolveAction(entry);
            if (action is null)
            {
                continue;
            }

            entries.Add(new PendingAuditEntry(
                EntityType: entry.Entity.GetType().Name,
                Action: action.Value,
                OldValues: action == AuditAction.Insert ? null : SerializeValues(entry, useOriginal: true),
                NewValues: action == AuditAction.Delete ? null : SerializeValues(entry, useOriginal: false),
                PrimaryKeyProperties: [.. entry.Properties.Where(p => p.Metadata.IsPrimaryKey())]));
        }

        return entries;
    }

    private static AuditAction? ResolveAction(EntityEntry entry)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                return AuditAction.Insert;
            case EntityState.Deleted:
                return AuditAction.Delete;
            case EntityState.Modified when entry.Entity is ISoftDeletable:
                var isDeletedProperty = entry.Property(nameof(ISoftDeletable.IsDeleted));
                var wasDeleted = isDeletedProperty.OriginalValue is true;
                var isDeleted = isDeletedProperty.CurrentValue is true;
                return !wasDeleted && isDeleted ? AuditAction.Delete : AuditAction.Update;
            case EntityState.Modified:
                return AuditAction.Update;
            default:
                return null;
        }
    }

    private static string? SerializeValues(EntityEntry entry, bool useOriginal)
    {
        var values = new Dictionary<string, object?>();

        foreach (var property in entry.Properties)
        {
            var propertyName = property.Metadata.Name;
            if (SensitivePropertyNames.Contains(propertyName))
            {
                continue;
            }

            // Update'te yalnızca fiilen değişen alanlar; Insert/Delete'te tüm alanlar taşınır.
            if (entry.State == EntityState.Modified && !property.IsModified)
            {
                continue;
            }

            values[propertyName] = useOriginal ? property.OriginalValue : property.CurrentValue;
        }

        return values.Count == 0 ? null : JsonSerializer.Serialize(values, JsonOptions);
    }

    private sealed record PendingAuditEntry(
        string EntityType,
        AuditAction Action,
        string? OldValues,
        string? NewValues,
        IReadOnlyList<PropertyEntry> PrimaryKeyProperties)
    {
        public AuditLog ToAuditLog(int? userId, DateTime timestampUtc) => new()
        {
            UserId = userId,
            EntityType = EntityType,
            EntityId = string.Join(",", PrimaryKeyProperties.Select(p => p.CurrentValue?.ToString() ?? string.Empty)),
            Action = Action,
            TimestampUtc = timestampUtc,
            OldValues = OldValues,
            NewValues = NewValues,
        };
    }
}
