using Autofac;
using Autofac.Extras.DynamicProxy;
using Core.Aspects.Autofac;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Xunit;

namespace Business.Tests;

/// <summary>
/// docs/MIMARI.md · SecuredOperationAspectHandler'ın en karmaşık parçası: reddedilen izin,
/// intercept edilen metodun gerçek dönüş tipine (IResult veya IDataResult&lt;T&gt;) göre doğru
/// Forbidden sonucunu reflection ile üretmeli. AspectPipelineTests'teki gerçek Autofac container
/// deseni burada iki farklı dönüş şekli için tekrarlanır.
/// </summary>
public class SecuredOperationAspectHandlerTests
{
    public interface ITestSecuredService
    {
        [SecuredOperation("required.permission")]
        Task<IResult> DoActionAsync();

        [SecuredOperation("required.permission")]
        Task<IDataResult<string>> GetDataAsync();
    }

    private sealed class TestSecuredService : ITestSecuredService
    {
        public Task<IResult> DoActionAsync() => Task.FromResult<IResult>(Result.Success());

        public Task<IDataResult<string>> GetDataAsync() => Task.FromResult<IDataResult<string>>(DataResult<string>.Success("ok"));
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public int? UserId => 1;

        public bool IsAuthenticated => true;

        public IReadOnlyCollection<string> Permissions => [];
    }

    private static IContainer BuildContainer()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<SecuredOperationAspectHandler>();
        builder.RegisterType<AspectDispatchInterceptor>();
        builder.RegisterInstance(new FakeCurrentUser()).As<ICurrentUser>();
        builder.RegisterType<TestSecuredService>()
            .As<ITestSecuredService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        return builder.Build();
    }

    [Fact(DisplayName = "IResult dönen metot izinsizken Forbidden Result üretir")]
    public async Task IResult_Donen_Metot_Forbidden_Uretir()
    {
        using var container = BuildContainer();
        var service = container.Resolve<ITestSecuredService>();

        var result = await service.DoActionAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact(DisplayName = "IDataResult<T> dönen metot izinsizken Forbidden DataResult<T> üretir")]
    public async Task IDataResult_Donen_Metot_Forbidden_Uretir()
    {
        using var container = BuildContainer();
        var service = container.Resolve<ITestSecuredService>();

        var result = await service.GetDataAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Null(result.Data);
    }
}
