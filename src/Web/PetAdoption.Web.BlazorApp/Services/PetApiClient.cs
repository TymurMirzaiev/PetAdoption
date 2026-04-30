namespace PetAdoption.Web.BlazorApp.Services;

public class PetApiClient
{
    private readonly HttpClient _http;

    public PetApiClient(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("PetApi");
    }
}
