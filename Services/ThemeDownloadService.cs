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
        private readonly YtDlpManager _ytDlpManager;

        public ThemeDownloadService(
            ILogger<ThemeDownloadService> logger,
            YtDlpManager ytDlpManager)
        {
            _logger = logger;
            _ytDlpManager = ytDlpManager;
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

            var isMovie = string.Equals(mediaType, "Movie", StringComparison.OrdinalIgnoreCase);
            var cleanTitle = title.Trim();
            var yearSuffix = productionYear.HasValue ? $" {productionYear.Value}" : string.Empty;

            // YouTube search ranking is query-dependent. Use several deliberately different
            // theme/opening queries so one poor ranking does not hide the actual opening song.
            // Avoid quoted phrases here: YouTube's search index can be more restrictive when
            // the entire Jellyfin title is treated as an exact phrase.
            var queries = isMovie
                ? new[]
                {
                    $"{cleanTitle} theme song",
                    $"{cleanTitle} main theme",
                    $"{cleanTitle} soundtrack theme",
                    $"{cleanTitle} official theme{yearSuffix}",
                    $"{cleanTitle} instrumental theme",
                    $"{cleanTitle} OST theme"
                }
                : new[]
                {
                    $"{cleanTitle} opening theme song",
                    $"{cleanTitle} opening song",
                    $"{cleanTitle} OP opening",
                    $"{cleanTitle} official opening",
                    $"{cleanTitle} opening full",
                    $"{cleanTitle} opening creditless",
                    $"{cleanTitle} ending theme song",
                    $"{cleanTitle} theme song",
                    $"{cleanTitle} OST opening"
                };

            _logger.LogInformation(
                "Searching YouTube for theme songs for {Title} using {QueryCount} targeted queries",
                cleanTitle,
                queries.Length);

            // Run a small number of searches concurrently. This keeps the request count
            // bounded while avoiding the latency of waiting for every query in sequence.
            using var queryThrottle = new SemaphoreSlim(3, 3);
            var queryTasks = queries.Select(query => SearchYouTubeQueryAsync(
                query,
                cleanTitle,
                isMovie,
                queryThrottle,
                cancellationToken));

            try
            {
                var queryResults = await Task.WhenAll(queryTasks);
                cancellationToken.ThrowIfCancellationRequested();

                var results = new List<YouTubeSearchResult>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var queryResult in queryResults)
                {
                    foreach (var result in queryResult)
                    {
                        if (seen.Add(result.VideoId))
                        {
                            results.Add(result);

                            if (results.Count >= 100)
                            {
                                break;
                            }
                        }
                    }

                    if (results.Count >= 100)
                    {
                        break;
                    }
                }

                return results
                    .OrderByDescending(r => r.MatchScore)
                    .ThenBy(r => r.DurationSeconds <= 0 ? double.MaxValue : r.DurationSeconds)
                    .Take(10)
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

        private async Task<List<YouTubeSearchResult>> SearchYouTubeQueryAsync(
            string query,
            string title,
            bool isMovie,
            SemaphoreSlim queryThrottle,
            CancellationToken cancellationToken)
        {
            await queryThrottle.WaitAsync(cancellationToken);

            try
            {
                _logger.LogInformation("YouTube theme search query: {Query}", query);

                var results = new List<YouTubeSearchResult>();
                var queryCount = 0;

                await foreach (var video in _youtube.Search.GetVideosAsync(query, cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var videoId = video.Id.Value;
                    if (string.IsNullOrWhiteSpace(videoId))
                    {
                        continue;
                    }

                    var score = ScoreThemeResult(title, video.Title, video.Duration, isMovie);
                    results.Add(new YouTubeSearchResult
                    {
                        VideoId = videoId,
                        Title = video.Title,
                        Channel = video.Author?.ChannelTitle ?? string.Empty,
                        DurationSeconds = video.Duration?.TotalSeconds ?? 0,
                        Url = $"https://www.youtube.com/watch?v={videoId}",
                        ThumbnailUrl = $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg",
                        MatchScore = score
                    });

                    // Pull enough candidates from every query to give the scorer a real choice.
                    if (++queryCount >= 15)
                    {
                        break;
                    }
                }

                return results;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // A single query failing should not make the whole search fail.
                _logger.LogWarning(ex, "YouTube theme search query failed: {Query}", query);
                return new List<YouTubeSearchResult>();
            }
            finally
            {
                queryThrottle.Release();
            }
        }

        private static int ScoreThemeResult(
            string title,
            string resultTitle,
            TimeSpan? duration,
            bool isMovie)
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

            if (normalizedResult.Contains("opening theme song", StringComparison.Ordinal))
            {
                score += 85;
            }
            else if (normalizedResult.Contains("opening theme", StringComparison.Ordinal))
            {
                score += 75;
            }
            else if (normalizedResult.Contains("opening song", StringComparison.Ordinal))
            {
                score += 70;
            }
            else if (normalizedResult.Contains("opening", StringComparison.Ordinal))
            {
                score += 50;
            }

            if (normalizedResult.Contains("official opening", StringComparison.Ordinal))
            {
                score += 30;
            }

            if (normalizedResult.Contains("creditless opening", StringComparison.Ordinal) ||
                normalizedResult.Contains("opening creditless", StringComparison.Ordinal))
            {
                score += 35;
            }

            if (normalizedResult.Contains("op ", StringComparison.Ordinal) ||
                normalizedResult.EndsWith(" op", StringComparison.Ordinal))
            {
                score += 25;
            }

            if (normalizedResult.Contains("ending theme", StringComparison.Ordinal) ||
                normalizedResult.Contains("ending song", StringComparison.Ordinal))
            {
                score += isMovie ? 15 : 35;
            }

            if (normalizedResult.Contains("theme song", StringComparison.Ordinal))
            {
                score += 50;
            }
            else if (normalizedResult.Contains("main theme", StringComparison.Ordinal))
            {
                score += 45;
            }
            else if (normalizedResult.Contains("theme", StringComparison.Ordinal))
            {
                score += 30;
            }

            if (normalizedResult.Contains("soundtrack", StringComparison.Ordinal) ||
                normalizedResult.Contains("ost", StringComparison.Ordinal))
            {
                score += 15;
            }

            if (normalizedResult.Contains("song", StringComparison.Ordinal) ||
                normalizedResult.Contains("music", StringComparison.Ordinal))
            {
                score += 10;
            }

            // Strongly demote common non-theme search results.
            if (normalizedResult.Contains("trailer", StringComparison.Ordinal))
            {
                score -= 80;
            }

            if (normalizedResult.Contains("recap", StringComparison.Ordinal) ||
                normalizedResult.Contains("review", StringComparison.Ordinal) ||
                normalizedResult.Contains("explained", StringComparison.Ordinal))
            {
                score -= 70;
            }

            if (normalizedResult.Contains("full episode", StringComparison.Ordinal) ||
                normalizedResult.Contains("episode", StringComparison.Ordinal))
            {
                score -= 65;
            }

            if (normalizedResult.Contains("top 10", StringComparison.Ordinal) ||
                normalizedResult.Contains("top 20", StringComparison.Ordinal) ||
                normalizedResult.Contains("characters", StringComparison.Ordinal) ||
                normalizedResult.Contains("reaction", StringComparison.Ordinal))
            {
                score -= 55;
            }

            if (duration.HasValue)
            {
                if (duration.Value <= TimeSpan.FromMinutes(5))
                {
                    score += 30;
                }
                else if (duration.Value <= TimeSpan.FromMinutes(10))
                {
                    score += 15;
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

                // YouTube can return a playable video with no audio-only streams.
                // Fall back to yt-dlp when it is available on the Jellyfin server.
                var audioStreams = streamManifest.GetAudioOnlyStreams().ToList();
                if (audioStreams.Count == 0)
                {
                    _logger.LogWarning(
                        "YouTube returned no audio-only streams for {VideoId}. Attempting yt-dlp fallback.",
                        videoId);

                    var ytDlpOutput = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp3");
                    if (await TryDownloadWithYtDlpAsync(videoId, ytDlpOutput, bitrate, cancellationToken))
                    {
                        try
                        {
                            Directory.CreateDirectory(outputDirectory);
                            var outputPath = Path.Combine(outputDirectory, "theme.mp3");
                            File.Move(ytDlpOutput, outputPath, true);

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

                            var metadataPath = Path.Combine(outputDirectory, "theme.json");
                            await File.WriteAllTextAsync(
                                metadataPath,
                                JsonSerializer.Serialize(metadata, new JsonSerializerOptions
                                {
                                    WriteIndented = true
                                }),
                                cancellationToken);

                            _logger.LogInformation(
                                "Theme song downloaded successfully with yt-dlp fallback: {Title} ({TargetType})",
                                video.Title,
                                targetType);
                            return metadata;
                        }
                        finally
                        {
                            if (File.Exists(ytDlpOutput))
                            {
                                try
                                {
                                    File.Delete(ytDlpOutput);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to delete yt-dlp temporary file {TempFile}", ytDlpOutput);
                                }
                            }
                        }
                    }

                    throw new InvalidOperationException(
                        $"YouTube did not provide a downloadable audio stream for video '{videoId}'. " +
                        "Install/update yt-dlp on the Jellyfin server and make sure it is available in PATH " +
                        "or set the YT_DLP_PATH environment variable, then try again.");
                }

                var audioStreamInfo = audioStreams.GetWithHighestBitrate();
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

        private async Task<bool> TryDownloadWithYtDlpAsync(
            string videoId,
            string outputPath,
            int bitrate,
            CancellationToken cancellationToken)
        {
            try
            {
                var ytDlpPath = await _ytDlpManager.GetExecutableAsync(cancellationToken);
                var outputTemplate = Path.Combine(
                    Path.GetDirectoryName(outputPath) ?? Path.GetTempPath(),
                    Path.GetFileNameWithoutExtension(outputPath) + ".%(ext)s");

                using var process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ytDlpPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                process.StartInfo.ArgumentList.Add("--no-playlist");
                process.StartInfo.ArgumentList.Add("--no-warnings");
                process.StartInfo.ArgumentList.Add("--no-update");
                process.StartInfo.ArgumentList.Add("-x");
                process.StartInfo.ArgumentList.Add("--audio-format");
                process.StartInfo.ArgumentList.Add("mp3");
                process.StartInfo.ArgumentList.Add("--audio-quality");
                process.StartInfo.ArgumentList.Add($"{bitrate}K");

                var ffmpegPath = GetFfmpegPath();
                if (File.Exists(ffmpegPath))
                {
                    process.StartInfo.ArgumentList.Add("--ffmpeg-location");
                    process.StartInfo.ArgumentList.Add(ffmpegPath);
                }

                process.StartInfo.ArgumentList.Add("-o");
                process.StartInfo.ArgumentList.Add(outputTemplate);
                process.StartInfo.ArgumentList.Add($"https://www.youtube.com/watch?v={videoId}");

                _logger.LogInformation(
                    "Trying managed yt-dlp fallback for YouTube video {VideoId} using {Path}",
                    videoId,
                    ytDlpPath);
                process.Start();

                var standardOutputTask = process.StandardOutput.ReadToEndAsync();
                var standardErrorTask = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync(cancellationToken);

                var standardOutput = await standardOutputTask;
                var standardError = await standardErrorTask;

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning(
                        "yt-dlp fallback failed for {VideoId} with exit code {ExitCode}: {Error}",
                        videoId,
                        process.ExitCode,
                        standardError);
                    return false;
                }

                var generatedFile = Path.Combine(
                    Path.GetDirectoryName(outputPath) ?? Path.GetTempPath(),
                    Path.GetFileNameWithoutExtension(outputPath) + ".mp3");

                if (!File.Exists(generatedFile))
                {
                    _logger.LogWarning(
                        "yt-dlp reported success for {VideoId}, but the expected MP3 was not created. Output: {Output}",
                        videoId,
                        standardOutput);
                    return false;
                }

                if (!string.Equals(generatedFile, outputPath, StringComparison.Ordinal))
                {
                    File.Move(generatedFile, outputPath, true);
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected yt-dlp fallback error for YouTube video {VideoId}", videoId);
                return false;
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
