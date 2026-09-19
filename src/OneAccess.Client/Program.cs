using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OneAccess.Client;
using OneAccess.Client.Services;
using OneAccess.Client.Services.Api;
using OneAccess.Client.Services.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Core Services
builder.Services.AddSingleton<ToastService>();
builder.Services.AddTransient<ApiResponseHandler>();

// Named HttpClient with ApiResponseHandler DelegatingHandler
builder.Services.AddHttpClient("OneAccessApi", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
})
.AddHttpMessageHandler<ApiResponseHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("OneAccessApi"));

// Auth Services
builder.Services.AddScoped<CookieAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CookieAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

// Typed API Clients
builder.Services.AddScoped<IUserApi, UserApi>();
builder.Services.AddScoped<IRoleApi, RoleApi>();
builder.Services.AddScoped<IPermissionApi, PermissionApi>();
builder.Services.AddScoped<IDivisionApi, DivisionApi>();

await builder.Build().RunAsync();
