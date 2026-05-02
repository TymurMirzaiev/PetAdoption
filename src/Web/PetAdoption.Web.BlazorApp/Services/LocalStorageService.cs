namespace PetAdoption.Web.BlazorApp.Services;

using Microsoft.JSInterop;

public interface ILocalStorageService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task RemoveAsync(string key);
}

public class LocalStorageService : ILocalStorageService
{
    private readonly IJSRuntime _js;

    public LocalStorageService(IJSRuntime js) => _js = js;

    public Task<string?> GetAsync(string key) =>
        _js.InvokeAsync<string?>("localStorage.getItem", key).AsTask();

    public Task SetAsync(string key, string value) =>
        _js.InvokeVoidAsync("localStorage.setItem", key, value).AsTask();

    public Task RemoveAsync(string key) =>
        _js.InvokeVoidAsync("localStorage.removeItem", key).AsTask();
}
