using Castle.DynamicProxy;
using Core.DataAccess;
using Core.Utilities.Results;

namespace Core.Aspects.Autofac;

/// <summary>
/// docs/MIMARI.md · A-06: EF BeginTransactionAsync; TransactionScope kullanılmaz.
/// Yalnızca exception'da değil, exception fırlatmadan dönen başarısız bir iş sonucunda
/// (ör. Result.Conflict) da rollback edilir — "hata yoksa commit et" kısmi yazımı gizler.
/// </summary>
public sealed class TransactionAspectHandler(IUnitOfWork unitOfWork) : IAspectHandler
{
    public async Task<TResult> HandleAsync<TResult>(IInvocation invocation, AspectAttribute attribute, Func<Task<TResult>> next)
    {
        var transaction = await unitOfWork.BeginTransactionAsync().ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            try
            {
                var result = await next().ConfigureAwait(false);

                if (result is IResult { IsSuccess: false })
                {
                    await transaction.RollbackAsync().ConfigureAwait(false);
                }
                else
                {
                    await transaction.CommitAsync().ConfigureAwait(false);
                }

                return result;
            }
            catch
            {
                await transaction.RollbackAsync().ConfigureAwait(false);
                throw;
            }
        }
    }
}
