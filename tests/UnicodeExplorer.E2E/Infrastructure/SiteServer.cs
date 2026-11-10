using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using TUnit.Core.Interfaces;

namespace UnicodeExplorer.E2E.Infrastructure;

/// <summary>
/// Serves the published site the way GitHub Pages does: under /unicode-explorer/, and unknown
/// paths return index.html with status 404 (the 404.html trick used for deep links).
/// Shared by all tests in a run.
/// </summary>
public sealed class SiteServer : IAsyncInitializer, IAsyncDisposable
{
    public const string BasePath = "/unicode-explorer/";

    private WebApplication? _app;

    /// <summary>e.g. http://127.0.0.1:54321/unicode-explorer/</summary>
    public string BaseUrl { get; private set; } = "";

    public string Url(string relativePath = "") => BaseUrl + relativePath.TrimStart('/');

    public async Task InitializeAsync()
    {
        var root = FindOrPublishSite();
        // The deploy workflow rewrites <base href> after publishing; do the same in memory
        var index = (await File.ReadAllTextAsync(Path.Combine(root, "index.html")))
            .Replace("<base href=\"/\" />", $"<base href=\"{BasePath}\" />");

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        _app = builder.Build();

        _app.Use(async (context, next) =>
        {
            if (context.Request.Path == BasePath || context.Request.Path == BasePath + "index.html")
                await WriteIndex(context, StatusCodes.Status200OK);
            else
                await next();
        });
        _app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(root),
            RequestPath = BasePath.TrimEnd('/'),
            ServeUnknownFileTypes = true,
        });
        _app.Run(context => WriteIndex(context, StatusCodes.Status404NotFound));

        await _app.StartAsync();
        var address = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        BaseUrl = address.TrimEnd('/') + BasePath;

        Task WriteIndex(HttpContext context, int status)
        {
            context.Response.StatusCode = status;
            context.Response.ContentType = "text/html; charset=utf-8";
            return context.Response.WriteAsync(index);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_app != null) await _app.DisposeAsync();
    }

    /// <summary>
    /// Uses E2E_SITE_DIR (the publish output's wwwroot, set by CI) when given.
    /// Otherwise publishes the app to &lt;repo&gt;/publish so local runs always test the current code.
    /// </summary>
    private static string FindOrPublishSite()
    {
        var configured = Environment.GetEnvironmentVariable("E2E_SITE_DIR");
        if (!string.IsNullOrEmpty(configured))
        {
            var dir = Path.GetFullPath(configured);
            if (!File.Exists(Path.Combine(dir, "index.html")))
                throw new InvalidOperationException($"E2E_SITE_DIR '{dir}' does not contain index.html.");
            return dir;
        }

        var repo = FindRepoRoot();
        var output = Path.Combine(repo, "publish");
        var psi = new ProcessStartInfo("dotnet", ["publish", Path.Combine("src", "UnicodeExplorer"), "-c", "Release", "-o", output])
        {
            WorkingDirectory = repo,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        // Don't let the test host's MSBuild settings leak into the child build
        foreach (var name in new[] { "MSBUILD_EXE_PATH", "MSBuildExtensionsPath", "MSBuildSDKsPath" })
            psi.Environment.Remove(name);

        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromMinutes(10)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("dotnet publish did not finish within 10 minutes.");
        }
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"dotnet publish failed ({process.ExitCode}):\n{stdout.Result}\n{stderr.Result}");

        return Path.Combine(output, "wwwroot");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "global.json"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Could not find the repository root (global.json).");
    }
}
