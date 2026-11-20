using System.Diagnostics;
using System.Net.Sockets;

namespace RaceDay.Mvc.Services;

/// <summary>
/// Development-only helper. When the MVC app starts it makes sure the RaceDay API is running:
/// if nothing is listening on <c>ApiBaseUrl</c>, it launches <c>RaceDay.Api</c> in the background
/// and waits until it is ready before the MVC site starts serving pages. The API is stopped again
/// when the MVC app stops. Disable with <c>"AutoStartApi": false</c> in appsettings.
/// </summary>
public class ApiAutoStartService : IHostedService
{
    private static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(120);

    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;
    private readonly ILogger<ApiAutoStartService> _log;
    private Process? _api;

    public ApiAutoStartService(IConfiguration config, IHostEnvironment env, ILogger<ApiAutoStartService> log)
    {
        _config = config;
        _env = env;
        _log = log;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        if (!_env.IsDevelopment() || !_config.GetValue("AutoStartApi", true))
            return;

        var baseUrl = new Uri(_config["ApiBaseUrl"] ?? "http://127.0.0.1:5088/");

        // Already running (started from Visual Studio, a terminal, etc.)? Then there is nothing to do.
        // The short grace period covers both projects being started at the same moment.
        if (await WaitForPortAsync(baseUrl, GracePeriod, null, ct))
        {
            _log.LogInformation("RaceDay API already running at {Url}.", baseUrl);
            return;
        }

        var apiDir = FindApiDirectory();
        if (apiDir is null)
        {
            _log.LogWarning("Could not find the RaceDay.Api project folder, so the API was not started automatically. " +
                            "Start it manually or set the correct ApiBaseUrl.");
            return;
        }

        _log.LogInformation("Starting RaceDay API from {Dir} ...", apiDir);

        var psi = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = apiDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--no-launch-profile");
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        psi.Environment["ASPNETCORE_URLS"] = $"{baseUrl.Scheme}://{baseUrl.Host}:{baseUrl.Port}";
        psi.Environment["RACEDAY_PARENT_PID"] = Environment.ProcessId.ToString();

        try
        {
            _api = Process.Start(psi);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Could not run 'dotnet'. Is the .NET SDK installed and on your PATH?");
            return;
        }

        if (_api is null)
            return;

        _api.OutputDataReceived += (_, e) => { if (e.Data is not null) _log.LogInformation("[API] {Line}", e.Data); };
        _api.ErrorDataReceived += (_, e) => { if (e.Data is not null) _log.LogWarning("[API] {Line}", e.Data); };
        _api.BeginOutputReadLine();
        _api.BeginErrorReadLine();

        if (await WaitForPortAsync(baseUrl, StartupTimeout, _api, ct))
            _log.LogInformation("RaceDay API is ready at {Url}.", baseUrl);
        else if (_api.HasExited)
            _log.LogError("The RaceDay API stopped during startup (exit code {Code}). See the [API] log lines above.", _api.ExitCode);
        else
            _log.LogWarning("The RaceDay API did not respond within {Seconds}s; continuing anyway.", (int)StartupTimeout.TotalSeconds);
    }

    public Task StopAsync(CancellationToken ct)
    {
        try
        {
            if (_api is { HasExited: false })
            {
                _log.LogInformation("Stopping RaceDay API ...");
                _api.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not stop the RaceDay API process.");
        }
        return Task.CompletedTask;
    }

    private string? FindApiDirectory()
    {
        var dir = new DirectoryInfo(_env.ContentRootPath);
        while (dir is not null)
        {
            foreach (var relative in new[] { "RaceDay.Api", Path.Combine("src", "RaceDay.Api") })
            {
                var candidate = Path.Combine(dir.FullName, relative);
                if (File.Exists(Path.Combine(candidate, "RaceDay.Api.csproj")))
                    return candidate;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private static async Task<bool> WaitForPortAsync(Uri url, TimeSpan timeout, Process? watch, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                using var client = new TcpClient();
                using var attempt = CancellationTokenSource.CreateLinkedTokenSource(ct);
                attempt.CancelAfter(500);
                await client.ConnectAsync(url.Host, url.Port, attempt.Token);
                return true;
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                // Not listening yet.
            }

            if (watch is { HasExited: true } || DateTime.UtcNow >= deadline)
                return false;

            await Task.Delay(500, ct);
        }
    }
}
