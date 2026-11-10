using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using UnicodeExplorer;
using UnicodeExplorer.Services;
using UnicodeExplorer.Unicode;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Load the Unicode data once before the app starts, so every component can use it synchronously.
using var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var data = await http.GetStringAsync($"data/unicode.txt?v={DataFileParser.CacheKey}");
var db = DataFileParser.Parse(data);

builder.Services.AddSingleton(db);
builder.Services.AddSingleton<SearchService>();
builder.Services.AddSingleton<ToastService>();
builder.Services.AddScoped<ClipboardService>();
builder.Services.AddScoped<FontService>();
builder.Services.AddSingleton<PreferenceService>();

await builder.Build().RunAsync();
