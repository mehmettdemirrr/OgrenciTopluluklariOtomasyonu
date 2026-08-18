using Core.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public sealed class EfDatabaseMigrator(AppDbContext context) : IDatabaseMigrator
{
    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        context.Database.MigrateAsync(cancellationToken);
}
