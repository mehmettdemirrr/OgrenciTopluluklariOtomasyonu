namespace Core.Utilities.Results;

/// <summary>
/// docs/MIMARI.md · A-03: Business IResult döner, controller'daki tek yardımcı
/// bunu HTTP koduna çevirir. Bu enum o eşlemenin anahtarıdır.
/// </summary>
public enum ResultStatus
{
    Success = 0,
    ValidationError = 1,
    Forbidden = 2,
    NotFound = 3,
    Conflict = 4,
}
