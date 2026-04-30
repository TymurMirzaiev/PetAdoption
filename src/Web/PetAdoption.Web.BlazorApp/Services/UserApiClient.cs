namespace PetAdoption.Web.BlazorApp.Services;

public class UserApiClient
{
    private readonly HttpClient _http;

    public UserApiClient(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("UserApi");
    }
}
