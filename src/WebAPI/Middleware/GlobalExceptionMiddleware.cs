using System.Net;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace WebAPI.Middleware;

/// <summary>docs/MIMARI.md · Y-25 / Y-28: beklenmeyen hatalar tek yerden, detay sızdırmadan yönetilir.</summary>
public sealed class GlobalExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var correlationId = context.TraceIdentifier;

            Log.Error(exception, "Beklenmeyen hata. CorrelationId: {CorrelationId}", correlationId);

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var problemDetails = new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Beklenmeyen bir hata oluştu.",
                Detail = $"Destek ekibine başvururken bu kimliği paylaşın: {correlationId}",
            };

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}
