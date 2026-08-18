using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WebAPI.IntegrationTests;

public class UygulamaAyagaKalkarTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact(DisplayName = "Uygulama ayağa kalkar ve Swagger uç noktası 200 döner")]
    public async Task Swagger_Endpoint_200_Doner()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
