using Autofac;
using Autofac.Extras.DynamicProxy;
using Core.Aspects.Autofac;
using Core.Utilities.Results;
using FluentValidation;
using Xunit;

namespace Business.Tests;

public class ValidationAspectHandlerTests
{
    private sealed class CallFlag
    {
        public bool Called;
    }

    public sealed class TestDto
    {
        public int Value { get; set; }
    }

    public sealed class TestDtoValidator : AbstractValidator<TestDto>
    {
        public TestDtoValidator() => RuleFor(x => x.Value).GreaterThan(0);
    }

    public interface ITestValidatableService
    {
        [ValidationAspect(typeof(TestDtoValidator))]
        Task<IResult> DoAsync(TestDto dto);
    }

    private sealed class TestValidatableService(CallFlag flag) : ITestValidatableService
    {
        public Task<IResult> DoAsync(TestDto dto)
        {
            flag.Called = true;
            return Task.FromResult<IResult>(Result.Success());
        }
    }

    private static (IContainer Container, CallFlag Flag) BuildContainer()
    {
        var flag = new CallFlag();
        var builder = new ContainerBuilder();
        builder.RegisterInstance(flag);
        builder.RegisterType<TestDtoValidator>().AsSelf();
        builder.RegisterType<ValidationAspectHandler>();
        builder.RegisterType<AspectDispatchInterceptor>();
        builder.RegisterType<TestValidatableService>()
            .As<ITestValidatableService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AspectDispatchInterceptor))
            .InstancePerLifetimeScope();

        return (builder.Build(), flag);
    }

    [Fact(DisplayName = "Geçerli DTO metodu çalıştırır")]
    public async Task GecerliDto_MetoduCalistirir()
    {
        var (container, flag) = BuildContainer();
        using (container)
        {
            var service = container.Resolve<ITestValidatableService>();
            var result = await service.DoAsync(new TestDto { Value = 5 });

            Assert.True(result.IsSuccess);
            Assert.True(flag.Called);
        }
    }

    [Fact(DisplayName = "Geçersiz DTO ValidationError döner, metodu hiç çalıştırmaz")]
    public async Task GecersizDto_ValidationErrorDoner()
    {
        var (container, flag) = BuildContainer();
        using (container)
        {
            var service = container.Resolve<ITestValidatableService>();
            var result = await service.DoAsync(new TestDto { Value = 0 });

            Assert.False(result.IsSuccess);
            Assert.Equal(ResultStatus.ValidationError, result.Status);
            Assert.False(flag.Called);
        }
    }
}
