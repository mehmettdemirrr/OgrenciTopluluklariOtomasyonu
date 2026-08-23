using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebAPI.Security;

/// <summary>
/// docs/MIMARI.md · A-53 × Y-25: oran sınırı reddi de diğer tüm hatalarla aynı gövde formatını
/// (RFC 7807 ProblemDetails) kullanır — çıplak 429 dönmez.
///
/// Program.cs'te satır içi async lambda olarak yazılmamasının sebebi: üst düzey deyimlerdeki
/// async lambda'nın derleyici state machine'i `Program/&lt;&gt;c` altına nested edilir ve
/// AsyncHygieneTests'in [CompilerGenerated] filtresinden kaçarak Y-27 ihlali gibi görünür.
/// </summary>
public static class RateLimitRejectionHandler
{
    public static async ValueTask HandleAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
        }

        // contentType parametresi zorunlu: WriteAsJsonAsync önceden set edilmiş Response.ContentType'ı
        // "application/json" ile ezer ve RFC 7807 gövdesi yanlış tiple gider.
        await response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Çok fazla istek gönderildi.",
                Detail = "Kısa sürede çok sayıda istek yaptınız. Lütfen bir süre bekleyip tekrar deneyin.",
            },
            options: null,
            contentType: "application/problem+json",
            cancellationToken);
    }
}
