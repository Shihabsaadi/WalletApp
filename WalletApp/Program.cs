using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WalletApp;
using WalletApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddLocalStorageServices();
builder.Services.AddSingleton<GoogleSheetsService>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<ExportService>();

await builder.Build().RunAsync();