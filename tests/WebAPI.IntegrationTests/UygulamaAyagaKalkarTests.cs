using System.Net;
using Xunit;

namespace WebAPI.IntegrationTests;

[Collection("WebAPI Integration Tests")]
public class UygulamaAyagaKalkarTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact(DisplayName = "Uygulama ayağa kalkar ve Swagger uç noktası 200 döner")]
    public async Task Swagger_Endpoint_200_Doner()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
