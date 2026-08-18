using Business.Abstract;
using Core.Utilities.Results;

namespace Business.Concrete;

public sealed class DiagnosticsManager : IDiagnosticsService
{
    public Task<IDataResult<string>> PingAsync() =>
        Task.FromResult<IDataResult<string>>(DataResult<string>.Success("pong"));

    public Task<IDataResult<string>> SecurePingAsync() =>
        Task.FromResult<IDataResult<string>>(DataResult<string>.Success("secure-pong"));
}
