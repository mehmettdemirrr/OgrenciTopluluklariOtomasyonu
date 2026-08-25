namespace Business.Abstract;

/// <summary>
/// docs/MIMARI.md · Y-68 (K-34, A-58): demo veri üretimi. Referans verisinin (fakülte/bölüm)
/// aksine <b>HasData ile üretilemez</b> — parola hash'i UserManager gerektirir ve tarihler
/// "bugüne göre" olmalıdır (geçmiş/gelecek etkinlik ayrımı sabit literalle bir yıl sonra bozulur).
/// Bu yüzden config kapılı bir seeder'dır ve ürettiği her satırı künyeler.
/// </summary>
public interface IDemoDataSeeder
{
    Task<DemoSeedOutcome> SeedAsync(CancellationToken cancellationToken = default);
}

/// <summary>Seeder'ın ne yaptığı — çağıran (Program.cs) bunu loglar.</summary>
public enum DemoSeedOutcome
{
    /// <summary><c>Seed:Demo</c> açık değil — Y-68'in üretim koruması.</summary>
    Disabled,

    /// <summary><c>Seed:DemoPassword</c> yok. Y-20: parola yalnızca user-secrets/ortam değişkeninden gelir.</summary>
    PasswordMissing,

    /// <summary>Künye tablosu dolu — demo veri zaten var, hiçbir satır üretilmedi (idempotanlık).</summary>
    AlreadyPresent,

    /// <summary>Demo veri sıfırdan üretildi.</summary>
    Created,

    /// <summary><c>Seed:ResetDemo</c> ile önceki demo veri silinip yeniden üretildi.</summary>
    Recreated,
}
