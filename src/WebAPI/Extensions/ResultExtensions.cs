using Core.Utilities.Results;
using Microsoft.AspNetCore.Mvc;
// Microsoft.AspNetCore.Http.IResult (Minimal API), Sdk.Web'in örtük using'leriyle her yerde
// devrede olduğu için Core.Utilities.Results.IResult ile isim çakışıyor; burada açıkça ayırıyoruz.
using IResult = Core.Utilities.Results.IResult;

namespace WebAPI.Extensions;

/// <summary>
/// docs/MIMARI.md · A-03: Business'ın döndürdüğü IResult'ı HTTP koduna çeviren tek yardımcı.
/// Controller'lar bu extension dışında hiçbir HTTP kod kararı vermez (Y-01).
/// </summary>
public static class ResultExtensions
{
    public static IActionResult ToActionResult(this IResult result) =>
        result.IsSuccess
            ? new OkObjectResult(new { message = result.Message })
            : ToProblemResult(result);

    public static IActionResult ToActionResult<T>(this IDataResult<T> result) =>
        result.IsSuccess
            ? new OkObjectResult(result.Data)
            : ToProblemResult(result);

    private static ObjectResult ToProblemResult(IResult result)
    {
        var statusCode = result.Status switch
        {
            ResultStatus.ValidationError => StatusCodes.Status400BadRequest,
            ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ResultStatus.NotFound => StatusCodes.Status404NotFound,
            ResultStatus.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };

        return new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = result.Message ?? "İstek işlenemedi.",
        })
        {
            StatusCode = statusCode,
        };
    }
}
