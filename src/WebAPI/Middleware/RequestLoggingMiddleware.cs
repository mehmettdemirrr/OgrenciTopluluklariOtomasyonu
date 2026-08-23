using System.Diagnostics;
using System.Security.Claims;
using Business.Abstract;
using Business.DTOs.Traffic;
using Microsoft.AspNetCore.WebUtilities;
using Serilog;

namespace WebAPI.Middleware;

/// <summary>
/// docs/PLAN-V3.md · K-28/A-44/O-2: yalnızca yazma istekleri (POST/PUT/DELETE), `/api/auth/*` ve
/// 4xx/5xx dönen istekler kaydedilir. `/api/files`, `/api/public`, `/hangfire` ve başarılı GET'ler
/// hiç kaydedilmez. Y-59: gövde/header/token asla yazılmaz; sorgu dizesindeki bilinen hassas
/// anahtarlar redakte edilir — SideNav'ın `?access_token=...` içeren Hangfire linki bunun canlı örneği.
///
/// Yazma, isteğin kendi DI scope'undan bağımsız yeni bir scope açarak yapılır (Y-43'ün mantığı):
/// iş transaction'ı geri alınsa/patlasa bile erişim izi satırı kalıcı olmalı.
/// </summary>
public sealed class RequestLoggingMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
{
    private static readonly string[] ExcludedPathPrefixes = ["/api/files", "/api/public", "/hangfire"];
    private static readonly HashSet<string> WriteMethods = new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "DELETE", "PATCH" };
    private static readonly HashSet<string> SensitiveQueryKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "access_token", "token", "password", "code",
    };

    private const string RedactedMarker = "[REDACTED]";

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (ExcludedPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        // Bir istisna next() içinde fırlarsa aşağıdaki satır hiç çalışmaz; finally bloğu yine de
        // devreye girer ve bu varsayılan (500) durum koduyla kayıt tutulur — GlobalExceptionMiddleware
        // bu middleware'in DIŞINDA olsa bile (Program.cs'te önce kayıt edilir) doğru sonuç elde edilir.
        var statusCode = StatusCodes.Status500InternalServerError;
        try
        {
            await next(context).ConfigureAwait(false);
            statusCode = context.Response.StatusCode;
        }
        finally
        {
            stopwatch.Stop();

            var method = context.Request.Method;
            if (WriteMethods.Contains(method) || path.StartsWith("/api/auth/", StringComparison.OrdinalIgnoreCase) || statusCode >= 400)
            {
                await RecordAsync(context, method, path, statusCode, stopwatch.ElapsedMilliseconds).ConfigureAwait(false);
            }
        }
    }

    private async Task RecordAsync(HttpContext context, string method, string path, int statusCode, long durationMs)
    {
        try
        {
            var request = new RecordTrafficLogRequestDto
            {
                CorrelationId = context.TraceIdentifier,
                UserId = ResolveUserId(context),
                IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                UserAgent = Truncate(context.Request.Headers.UserAgent.ToString(), 512),
                HttpMethod = method,
                Path = TruncateRequired(path, 512),
                RedactedQueryString = RedactQueryString(context.Request.QueryString.Value),
                StatusCode = statusCode,
                DurationMs = durationMs,
            };

            // Y-43'ün mantığı: isteğin kendi DI scope'undan (ve DbContext'inden) bağımsız yeni bir
            // scope — iş transaction'ı geri alınsa bile erişim izi kalıcı olur.
            using var scope = scopeFactory.CreateScope();
            var trafficLogService = scope.ServiceProvider.GetRequiredService<ITrafficLogService>();
            await trafficLogService.RecordAsync(request, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Trafik logu yazımı asla asıl isteği etkilemez/maskeleyemez — sessizce loglanır.
            Log.Warning(ex, "Trafik log kaydı yazılamadı. CorrelationId: {CorrelationId}", context.TraceIdentifier);
        }
    }

    private static int? ResolveUserId(HttpContext context)
    {
        var claim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null && int.TryParse(claim.Value, out var id) ? id : null;
    }

    private static string? RedactQueryString(string? queryString)
    {
        if (string.IsNullOrEmpty(queryString))
        {
            return null;
        }

        var parsed = QueryHelpers.ParseQuery(queryString);
        var redacted = parsed.ToDictionary(
            kv => kv.Key,
            kv => SensitiveQueryKeys.Contains(kv.Key) ? RedactedMarker : kv.Value.ToString());

        var rebuilt = "?" + string.Join('&', redacted.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={(kv.Value == RedactedMarker ? RedactedMarker : Uri.EscapeDataString(kv.Value))}"));
        return Truncate(rebuilt, 1024);
    }

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) ? value : value.Length <= maxLength ? value : value[..maxLength];

    private static string TruncateRequired(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
