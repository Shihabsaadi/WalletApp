using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazored.LocalStorage;
using WalletApp;
using WalletApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// HTTP Client
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Local storage (saves login token in browser)
builder.Services.AddBlazoredLocalStorage();

// App services — all Scoped to match LocalStorage's lifetime
builder.Services.AddScoped<GoogleSheetsService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ExportService>();

// Load appsettings.json from wwwroot
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

await builder.Build().RunAsync();