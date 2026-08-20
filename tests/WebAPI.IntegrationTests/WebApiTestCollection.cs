using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// Hangfire'ın JobStorage.Current'ı (DI kaydı bile bu statiğe sarmalıyor) process genelinde tek —
/// aynı test sürecinde paralel çalışan birden fazla WebApplicationFactory (her biri kendi LocalDB'siyle)
/// bu statiği birbirinin üzerine yazar ve rastgele 500 hatalarına yol açar. Bu projedeki tüm test
/// sınıfları bu yüzden TEK collection'da, sıralı çalışır — üretimde zaten tek Program.cs örneği
/// çalıştığından bu gerçek bir kapsam kaybı değildir.
/// </summary>
[CollectionDefinition("WebAPI Integration Tests", DisableParallelization = true)]
public sealed class WebApiTestCollection;
