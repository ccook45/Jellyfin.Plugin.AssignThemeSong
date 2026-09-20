#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.xThemeSong.Services;

/// <summary>
/// Downloads and manages the official yt-dlp standalone binary used by xThemeSong.
/// </summary>
public sealed class YtDlpManager
{
    private const string LatestReleaseBaseUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/";
    private static readonly HttpClient HttpClient = CreateHttpClient();

    private readonly string _toolDirectory;
    private readonly ILogger<YtDlpManager> _logger;

    public YtDlpManager(
        IApplicationPaths applicationPaths,
        ILogger<YtDlpManager> logger)
    {
        _logger = logger;
        _toolDirectory = Path.Combine(
            applicationPaths.DataPath,
            "xThemeSong",
            "tools",
            "yt-dlp");
    }

    /// <summary>
    /// Gets a usable yt-dlp executable, downloading the official release when necessary.
    /// An explicit YT_DLP_PATH continues to take precedence for advanced installations.
    /// </summary>
    public async Task<string> GetExecutableAsync(CancellationToken cancellationToken)
    {
        var configuredPath = Environment.GetEnvironmentVariable("YT_DLP_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            _logger.LogInformation("Using yt-dlp from YT_DLP_PATH: {Path}", configuredPath);
            return configuredPath;
        }

        var managedPath = Path.Combine(_toolDirectory, GetExecutableFileName());

        if (File.Exists(managedPath) && await IsUsableAsync(managedPath, cancellationToken))
        {
            return managedPath;
        }

        await DownloadLatestAsync(managedPath, cancellationToken);

        if (!await IsUsableAsync(managedPath, cancellationToken))
        {
            throw new InvalidOperationException(
                $"xThemeSong downloaded yt-dlp to '{managedPath}', but the executable could not be started.");
        }

        return managedPath;
    }

    private async Task DownloadLatestAsync(string destinationPath, CancellationToken cancellationToken)
    {
        var assetName = GetAssetName();
        var checksumName = "SHA2-256SUMS";
        var binaryUrl = LatestReleaseBaseUrl + assetName;
        var checksumUrl = LatestReleaseBaseUrl + checksumName;

        Directory.CreateDirectory(_toolDirectory);

        var tempPath = destinationPath + "." + Guid.NewGuid().ToString("N") + ".download";
        var checksumPath = tempPath + ".checksums";

        try
        {
            _logger.LogInformation(
                "Downloading managed yt-dlp ({Asset}) from the official yt-dlp release.",
                assetName);

            using (var response = await HttpClient.GetAsync(
                binaryUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var output = File.Create(tempPath);
                await input.CopyToAsync(output, cancellationToken);
            }

            string checksumText;
            using (var response = await HttpClient.GetAsync(checksumUrl, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                checksumText = await response.Content.ReadAsStringAsync(cancellationToken);
            }

            var expectedHash = FindExpectedSha256(checksumText, assetName);
            if (string.IsNullOrWhiteSpace(expectedHash))
            {
                throw new InvalidOperationException(
                    $"The official yt-dlp checksum list did not contain an entry for '{assetName}'.");
            }

            var actualHash = await ComputeSha256Async(tempPath, cancellationToken);
            if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"The downloaded yt-dlp checksum did not match the official SHA-256 value. " +
                    $"Expected {expectedHash}, got {actualHash}.");
            }

            File.Move(tempPath, destinationPath, true);
            SetExecutablePermission(destinationPath);

            _logger.LogInformation(
                "Managed yt-dlp installed successfully at {Path}. SHA-256: {Sha256}",
                destinationPath,
                actualHash);
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
        finally
        {
            TryDelete(checksumPath);
        }
    }

    private async Task<bool> IsUsableAsync(string executablePath, CancellationToken cancellationToken)
    {
        try
        {
            SetExecutablePermission(executablePath);

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            // yt-dlp requires a URL unless --version/another informational option is supplied.
            // Use the actual version command so a successful exit proves the binary can execute.
            process.StartInfo.ArgumentList.Add("--version");
            process.Start();
            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                _logger.LogDebug("Managed yt-dlp is available: {Version}", output.Trim());
                return true;
            }

            _logger.LogWarning(
                "Managed yt-dlp executable failed its version check. Exit code {ExitCode}: {Error}",
                process.ExitCode,
                error.Trim());
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Managed yt-dlp executable is not usable: {Path}", executablePath);
            return false;
        }
    }

    private string GetAssetName()
    {
        if (OperatingSystem.IsWindows())
        {
            return System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture switch
            {
                System.Runtime.InteropServices.Architecture.Arm64 => "yt-dlp_arm64.exe",
                System.Runtime.InteropServices.Architecture.X86 => "yt-dlp_x86.exe",
                _ => "yt-dlp.exe"
            };
        }

        if (OperatingSystem.IsMacOS())
        {
            return "yt-dlp_macos";
        }

        if (OperatingSystem.IsLinux())
        {
            var architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
            var isMusl = File.Exists("/etc/alpine-release");

            return (isMusl, architecture) switch
            {
                (true, System.Runtime.InteropServices.Architecture.X64) => "yt-dlp_musllinux",
                (true, System.Runtime.InteropServices.Architecture.Arm64) => "yt-dlp_musllinux_aarch64",
                (false, System.Runtime.InteropServices.Architecture.X64) => "yt-dlp_linux",
                (false, System.Runtime.InteropServices.Architecture.Arm64) => "yt-dlp_linux_aarch64",
                _ => throw new PlatformNotSupportedException(
                    $"xThemeSong does not have a managed yt-dlp binary for Linux architecture '{architecture}'.")
            };
        }

        throw new PlatformNotSupportedException(
            "xThemeSong does not have a managed yt-dlp binary for this operating system.");
    }

    private static string GetExecutableFileName()
    {
        if (OperatingSystem.IsWindows())
        {
            return "yt-dlp.exe";
        }

        return "yt-dlp";
    }

    private static string FindExpectedSha256(string checksums, string assetName)
    {
        foreach (var line in checksums.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 &&
                string.Equals(parts[^1].TrimStart('*'), assetName, StringComparison.Ordinal))
            {
                return parts[0];
            }
        }

        return string.Empty;
    }

    private static async Task<string> ComputeSha256Async(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void SetExecutablePermission(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            try
            {
                File.SetUnixFileMode(
                    path,
                    UnixFileMode.UserRead |
                    UnixFileMode.UserWrite |
                    UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead |
                    UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead |
                    UnixFileMode.OtherExecute);
            }
            catch
            {
                // The subsequent version check will report a useful error if execution is not possible.
            }
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("xThemeSong/1.4.20");
        return client;
    }
}
