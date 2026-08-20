using Autofac;
using Autofac.Extras.DynamicProxy;
using Core.Aspects.Autofac;
using Core.DataAccess;
using Core.Utilities.Results;
using Xunit;

namespace Business.Tests;

/// <summary>
/// docs/MIMARI.md · A-06: "hata yoksa commit et" yeterli değil — exception fırlatmadan dönen
/// başarısız bir iş sonucu da rollback edilmeli.
/// </summary>
public class TransactionAspectHandlerTests
{
    private sealed class FakeTransaction : ITransaction
    {
        public bool Committed { get; private set; }

        public bool RolledBack { get; private set; }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Committed = true;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RolledBack = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeTransaction? LastTransaction { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            LastTransaction = new FakeTransaction();
            return Task.FromResult<ITransaction>(LastTransaction);
        }
    }

    public interface ITestTransactionalService
    {
        [TransactionAspect]
        Task<IResult> SucceedAsync();

        [TransactionAspect]
        Task<IResult> FailWithoutExceptionAsync();

        [TransactionAspect]
        Task<IResult> ThrowAsync();
    }

    private sealed class TestTransactionalService : ITestTransactionalService
    {
        public Task<IResult> SucceedAsync() => Task.FromResult<IResult>(Result.Success());

        public Task<IResult> FailWithoutExceptionAsync() => Task.FromResult<IResult>(Result.Conflict("çakışma"));

        public Task<IResult> ThrowAsync() => throw new InvalidOperationException("boom");
    }

    private static (IContainer Container, FakeUnitOfWork UnitOfWork) BuildContainer()
    {
        var unitOfWork = new FakeUnitOfWork();
        var builder = new ContainerBuilder();
        builder.RegisterInstance(unitOfWork).As<IUnitOfWork>();
        builder.RegisterType<TransactionAspectHandler>();
        builder.RegisterType<AspectDispatchInterceptor>();
        builder.RegisterType<TestTransactionalService>()
            .As<ITestTransactionalService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        return (builder.Build(), unitOfWork);
    }

    [Fact(DisplayName = "Başarılı sonuç commit edilir")]
    public async Task BasariliSonuc_CommitEdilir()
    {
        var (container, unitOfWork) = BuildContainer();
        using (container)
        {
            var service = container.Resolve<ITestTransactionalService>();
            await service.SucceedAsync();

            Assert.True(unitOfWork.LastTransaction!.Committed);
            Assert.False(unitOfWork.LastTransaction.RolledBack);
        }
    }

    [Fact(DisplayName = "Exception fırlatmadan dönen başarısız sonuç rollback edilir")]
    public async Task ExceptionsizBasarisizSonuc_RollbackEdilir()
    {
        var (container, unitOfWork) = BuildContainer();
        using (container)
        {
            var service = container.Resolve<ITestTransactionalService>();
            await service.FailWithoutExceptionAsync();

            Assert.True(unitOfWork.LastTransaction!.RolledBack);
            Assert.False(unitOfWork.LastTransaction.Committed);
        }
    }

    [Fact(DisplayName = "Exception rollback edilir ve yeniden fırlatılır")]
    public async Task Exception_RollbackEdilirVeYenidenFirlatilir()
    {
        var (container, unitOfWork) = BuildContainer();
        using (container)
        {
            var service = container.Resolve<ITestTransactionalService>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.ThrowAsync());

            Assert.True(unitOfWork.LastTransaction!.RolledBack);
            Assert.False(unitOfWork.LastTransaction.Committed);
        }
    }
}
