using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using PetAdoption.Web.BlazorApp;
using PetAdoption.Web.BlazorApp.Auth;
using PetAdoption.Web.BlazorApp.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<AuthorizationMessageHandler>();

builder.Services.AddHttpClient("PetApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiUrls:PetService"] ?? "http://localhost:8080");
}).AddHttpMessageHandler<AuthorizationMessageHandler>();

builder.Services.AddHttpClient("UserApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiUrls:UserService"] ?? "http://localhost:5001");
}).AddHttpMessageHandler<AuthorizationMessageHandler>();

builder.Services.AddHttpClient("UserApiDirect", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiUrls:UserService"] ?? "http://localhost:5001");
});

builder.Services.AddScoped<PetApiClient>();
builder.Services.AddScoped<UserApiClient>();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<IChatUnreadService, ChatUnreadService>();

await builder.Build().RunAsync();
