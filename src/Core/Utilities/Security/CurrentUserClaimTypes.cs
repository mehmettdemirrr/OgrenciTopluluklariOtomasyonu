namespace Core.Utilities.Security;

/// <summary>
/// JWT'ye izin claim'i yazan (AuthManager) ile onu okuyan (HttpContextCurrentUser) taraf
/// arasında claim tipi string'inin tek kaynağı.
/// </summary>
public static class CurrentUserClaimTypes
{
    public const string Permission = "permission";
}
