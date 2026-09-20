using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using Jellyfin.Plugin.xThemeSong.Models;
using System.Text.Json;

namespace Jellyfin.Plugin.xThemeSong.Services
{
    public class ThemeDownloadService
    {
        private readonly ILogger<ThemeDownloadService> _logger;
        private readonly YoutubeClient _youtube;

        public ThemeDownloadService(ILogger<ThemeDownloadService> logger)
        {
            _logger = logger;
            _youtube = new YoutubeClient();
        }

        /// <summary>
        /// Gets the FFmpeg path, checking user configuration, environment variables, and common locations.
        /// </summary>
        private string GetFfmpegPath()
        {
            // Priority 1: Check user-configured path (from plugin settings)
            var configuredPath = Plugin.Instance?.Configuration?.FFmpegPath;
            if (!string.IsNullOrEmpty(configuredPath))
            {
                if (File.Exists(configuredPath))
                {
                    _logger.LogInformation("Using user-configured FFmpeg path: {Path}", configuredPath);
                    return configuredPath;
                }
                else
                {
                    _logger.LogWarning("User-configured FFmpeg path does not exist: {Path}, falling back to auto-detection", configuredPath);
                }
            }

            // Priority 2: Check Jellyfin's environment variable (used in Docker)
            var jellyfinFfmpeg = Environment.GetEnvironmentVariable("JELLYFIN_FFMPEG");
            if (!string.IsNullOrEmpty(jellyfinFfmpeg) && File.Exists(jellyfinFfmpeg))
            {
                _logger.LogInformation("Using FFmpeg from JELLYFIN_FFMPEG environment variable: {Path}", jellyfinFfmpeg);
                return jellyfinFfmpeg;
            }

            // Priority 3: Check common FFmpeg locations
            var possiblePaths = new[]
            {
                // Linux/Docker Jellyfin locations
                "/usr/lib/jellyfin-ffmpeg/ffmpeg",
                "/usr/bin/ffmpeg",
                "/usr/local/bin/ffmpeg",
                
                // macOS locations
                "/opt/homebrew/bin/ffmpeg",
                "/usr/local/bin/ffmpeg",
                
                // Windows locations
                @"C:\Program Files\Jellyfin\Server\ffmpeg.exe",
                @"C:\ProgramData\chocolatey\bin\ffmpeg.exe",
                @"C:\ffmpeg\bin\ffmpeg.exe"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    _logger.LogInformation("Found FFmpeg at: {Path}", path);
                    return path;
                }
            }

            // Priority 4: Fall back to PATH - just use "ffmpeg" and let the OS find it
            var ffmpegName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
            _logger.LogInformation("Using FFmpeg from system PATH: {Name}", ffmpegName);
            return ffmpegName;
        }

        /// <summary>
        /// Searches YouTube for likely theme songs for a media item.
        /// The query follows the same approach as the reference theme downloader:
        /// title + media type + "theme song", while scoring short and title-matching results higher.
        /// </summary>
        public async Task<List<YouTubeSearchResult>> SearchYouTubeThemes(
            string title,
            string mediaType,
            int? productionYear = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return new List<YouTubeSearchResult>();
            }

            var typeLabel = string.Equals(mediaType, "Movie", StringComparison.OrdinalIgnoreCase)
                ? "movie"
                : "tv show";

            var cleanTitle = title.Trim();
            var query = $"{cleanTitle} {typeLabel} theme song";
            if (productionYear.HasValue && productionYear.Value > 0)
            {
                query = $"{cleanTitle} {productionYear.Value} {typeLabel} theme song";
            }

            _logger.LogInformation("Searching YouTube for theme song: {Query}", query);

            var results = new List<YouTubeSearchResult>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                await foreach (var video in _youtube.Search.GetVideosAsync(query, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var videoId = video.Id.Value;
                    if (string.IsNullOrWhiteSpace(videoId) || !seen.Add(videoId))
                    {
                        continue;
                    }

                    var score = ScoreThemeResult(cleanTitle, typeLabel, video.Title, video.Duration);
                    results.Add(new YouTubeSearchResult
                    {
                        VideoId = videoId,
                        Title = video.Title,
                        Channel = video.Author?.ChannelTitle ?? string.Empty,
                        Duration = video.Duration,
                        Url = $"https://www.youtube.com/watch?v={videoId}",
                        MatchScore = score
                    });

                    // We only need a small candidate set for the UI.
                    if (results.Count >= 12)
                    {
                        break;
                    }
                }

                return results
                    .OrderByDescending(r => r.MatchScore)
                    .ThenBy(r => r.Duration ?? TimeSpan.MaxValue)
                    .Take(6)
                    .ToList();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "YouTube theme search failed for {Title}", title);
                throw;
            }
        }

        private static int ScoreThemeResult(
            string title,
            string typeLabel,
            string resultTitle,
            TimeSpan? duration)
        {
            var normalizedTitle = NormalizeSearchText(title);
            var normalizedResult = NormalizeSearchText(resultTitle);
            var score = 0;

            if (!string.IsNullOrWhiteSpace(normalizedTitle) &&
                normalizedResult.Contains(normalizedTitle, StringComparison.Ordinal))
            {
                score += 100;
            }

            foreach (var token in normalizedTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (token.Length >= 3 && normalizedResult.Contains(token, StringComparison.Ordinal))
                {
                    score += 8;
                }
            }

            if (normalizedResult.Contains("theme", StringComparison.Ordinal))
            {
                score += 25;
            }

            if (normalizedResult.Contains("soundtrack", StringComparison.Ordinal) ||
                normalizedResult.Contains("ost", StringComparison.Ordinal))
            {
                score += 15;
            }

            if (normalizedResult.Contains(typeLabel, StringComparison.Ordinal))
            {
                score += 5;
            }

            if (duration.HasValue)
            {
                if (duration.Value <= TimeSpan.FromMinutes(5))
                {
                    score += 20;
                }
                else if (duration.Value <= TimeSpan.FromMinutes(10))
                {
                    score += 10;
                }
                else
                {
                    score -= 30;
                }
            }

            return score;
        }

        private static string NormalizeSearchText(string value)
        {
            var chars = value
                .ToLowerInvariant()
                .Select(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) ? c : ' ')
                .ToArray();

            return string.Join(
                ' ',
                new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Downloads a theme song from YouTube and saves it to the specified directory.
        /// </summary>
        /// <param name="input">YouTube video ID or URL</param>
        /// <param name="outputDirectory">Directory to save the theme song</param>
        /// <param name="bitrate">Audio bitrate in kbps</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="targetType">Target type: Movie, Series, Season, or BoxSet</param>
        /// <param name="parentId">Parent ID for inheritance (Series ID for seasons, Collection ID for BoxSets)</param>
        /// <returns>Theme metadata</returns>
        public async Task<ThemeMetadata> DownloadFromYouTube(
            string input, 
            string outputDirectory, 
            int bitrate, 
            CancellationToken cancellationToken,
            string targetType = "Movie",
            string? parentId = null)
        {
            try
            {
                // Extract video ID from URL or use directly if it's an ID
                string videoId = input;
                if (input.Contains("youtube.com") || input.Contains("youtu.be"))
                {
                    // Simple video ID extraction
                    if (input.Contains("youtube.com/watch?v="))
                    {
                        videoId = input.Split(new[] { "v=" }, StringSplitOptions.None)[1].Split('&')[0];
                    }
                    else if (input.Contains("youtu.be/"))
                    {
                        videoId = input.Split(new[] { "youtu.be/" }, StringSplitOptions.None)[1].Split('?')[0];
                    }
                }

                _logger.LogInformation("Downloading audio from YouTube video: {VideoId} for {TargetType}", videoId, targetType);

                // Get video details
                var video = await _youtube.Videos.GetAsync(videoId, cancellationToken);
                var streamManifest = await _youtube.Videos.Streams.GetManifestAsync(videoId, cancellationToken);
                
                // Get the best audio stream
                var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                
                // Create temporary file for downloaded audio
                var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{audioStreamInfo.Container.Name}");
                
                try
                {
                    // Download the audio
                    _logger.LogDebug("Downloading audio stream to temp file: {TempFile}", tempFile);
                    await _youtube.Videos.Streams.DownloadAsync(audioStreamInfo, tempFile, null, cancellationToken);

                    // Get FFmpeg path
                    var ffmpegPath = GetFfmpegPath();

                    // Output path for the theme song
                    var outputPath = Path.Combine(outputDirectory, "theme.mp3");

                    _logger.LogDebug("Converting to MP3 using FFmpeg: {FfmpegPath}", ffmpegPath);

                    // Convert to MP3 using FFmpeg with specified bitrate
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = ffmpegPath,
                            Arguments = $"-i \"{tempFile}\" -b:a {bitrate}k -vn \"{outputPath}\" -y",
                            UseShellExecute = false,
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    
                    // Read error output for debugging
                    var errorOutput = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync(cancellationToken);

                    if (process.ExitCode != 0)
                    {
                        _logger.LogError("FFmpeg conversion failed with exit code {ExitCode}. Error: {Error}", process.ExitCode, errorOutput);
                        throw new Exception($"FFmpeg conversion failed: {errorOutput}");
                    }

                    _logger.LogDebug("Successfully converted to MP3: {OutputPath}", outputPath);

                    // Create metadata with target type and parent ID
                    var metadata = new ThemeMetadata
                    {
                        YouTubeId = videoId,
                        YouTubeUrl = $"https://www.youtube.com/watch?v={videoId}",
                        Title = video.Title,
                        Uploader = video.Author.ChannelTitle,
                        DateAdded = DateTime.UtcNow,
                        DateModified = DateTime.UtcNow,
                        IsUserUploaded = false,
                        TargetType = targetType,
                        ParentId = parentId,
                        InheritFromParent = true
                    };

                    // Save metadata
                    var metadataPath = Path.Combine(outputDirectory, "theme.json");
                    await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    }), cancellationToken);

                    _logger.LogInformation("Theme song downloaded successfully for video: {Title} ({TargetType})", video.Title, targetType);
                    return metadata;
                }
                finally
                {
                    // Clean up temp file
                    if (File.Exists(tempFile))
                    {
                        try 
                        {
                            File.Delete(tempFile);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete temp file {TempFile}", tempFile);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading theme song from YouTube");
                throw;
            }
        }

        /// <summary>
        /// Saves an uploaded theme song to the specified directory.
        /// </summary>
        /// <param name="sourcePath">Path to the uploaded file</param>
        /// <param name="outputDirectory">Directory to save the theme song</param>
        /// <param name="bitrate">Audio bitrate in kbps</param>
        /// <param name="originalFileName">Original filename</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <param name="targetType">Target type: Movie, Series, Season, or BoxSet</param>
        /// <param name="parentId">Parent ID for inheritance</param>
        /// <returns>Theme metadata</returns>
        public async Task<ThemeMetadata> SaveUploadedTheme(
            string sourcePath, 
            string outputDirectory, 
            int bitrate, 
            string originalFileName, 
            CancellationToken cancellationToken,
            string targetType = "Movie",
            string? parentId = null)
        {
            try
            {
                // Get FFmpeg path
                var ffmpegPath = GetFfmpegPath();

                var outputPath = Path.Combine(outputDirectory, "theme.mp3");

                _logger.LogInformation("Converting uploaded file to MP3 using FFmpeg: {FfmpegPath}", ffmpegPath);

                // Convert/normalize uploaded MP3 using FFmpeg
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = $"-i \"{sourcePath}\" -b:a {bitrate}k -vn \"{outputPath}\" -y",
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                
                // Read error output for debugging
                var errorOutput = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                {
                    _logger.LogError("FFmpeg conversion failed with exit code {ExitCode}. Error: {Error}", process.ExitCode, errorOutput);
                    throw new Exception($"FFmpeg conversion failed: {errorOutput}");
                }

                _logger.LogDebug("Successfully converted uploaded file to MP3: {OutputPath}", outputPath);

                // Create metadata with target type and parent ID
                var metadata = new ThemeMetadata
                {
                    IsUserUploaded = true,
                    OriginalFileName = originalFileName,
                    DateAdded = DateTime.UtcNow,
                    DateModified = DateTime.UtcNow,
                    TargetType = targetType,
                    ParentId = parentId,
                    InheritFromParent = true
                };

                // Save metadata
                var metadataPath = Path.Combine(outputDirectory, "theme.json");
                await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                }), cancellationToken);

                _logger.LogInformation("Uploaded theme song saved successfully ({TargetType})", targetType);
                return metadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving uploaded theme song");
                throw;
            }
        }
    }
}
