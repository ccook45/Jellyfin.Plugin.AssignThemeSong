# xThemeSong

A Jellyfin 12 plugin that allows you to download theme songs from YouTube or upload custom MP3 files for your movies and TV shows.

<p align="center">
<img alt="Logo" src="https://raw.githubusercontent.com/ccook45/Jellyfin.Plugin.AssignThemeSong/main/images/icon.png" style="width:50%;" />
</p>

## ✨ Features

### Core Features
- 🎵 Download theme songs from YouTube by providing video ID or URL
- 📤 Upload your own MP3 files as theme songs
- 🎬 Supports both movies and TV shows
- 📁 Automatically saves theme songs as `theme.mp3` in media folders
- 📝 Stores metadata in `theme.json` files
- ⏰ Scheduled task to process theme songs
- 🗑️ Delete existing theme songs with confirmation

### User Interface
- 🎛️ Configuration page with **Settings** and **Media Management** tabs
- 🔄 Loading animations during processing
- 🎧 Audio players for existing theme songs
- ✅ Modern modal dialogs for success/error messages
- 📚 Media Management overview for large libraries
- 📄 **Paginated Media Management** - large libraries are rendered a page at a time to keep the UI responsive
- 🖼️ Lazy-loaded poster thumbnails and deferred audio loading in Media Management

### Advanced Features
- 📤 **Export/Import Theme Mappings** - Backup and migrate themes between servers
- 🔐 **Role-Based Access Control** - Control who can manage theme songs
- 👤 **Per-User Preferences** - Individual settings for enable/disable, volume, duration
- 📚 **Media Management** - View media with theme song status at a glance
- 📝 **Bulk YouTube URL Assignment** - Set URLs for multiple items in settings
- ⚙️ **Custom FFmpeg Path** - Configure FFmpeg location or use auto-detect

## 📋 Requirements

- **Jellyfin Server**: **Version 12.0.0 or later**
- **.NET**: **.NET 10**
- **File Transformation Plugin**: **REQUIRED** for Web UI features to work. Install from the [File Transformation plugin](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation)
- **FFmpeg**: Must be installed on your Jellyfin server (usually bundled with Jellyfin)
- **Internet Connection**: Required for YouTube downloads

## 🔧 Installation

### From the Jellyfin Plugin Repository

1. Add this repository URL to Jellyfin:
   `https://raw.githubusercontent.com/ccook45/Jellyfin.Plugin.AssignThemeSong/main/manifest.json`
2. Go to **Dashboard → Plugins → Repositories** and add the URL.
3. Go to **Dashboard → Plugins → Catalog**.
4. Search for **xThemeSong**.
5. Click **Install** and restart Jellyfin.

### Manual Installation

1. Download the latest release from [GitHub Releases](https://github.com/ccook45/Jellyfin.Plugin.AssignThemeSong/releases).
2. Extract the zip file.
3. Copy the contents to your Jellyfin plugins directory:
   - **Windows**: `%AppData%\Jellyfin\Server\plugins\xThemeSong`
   - **Linux**: `/var/lib/jellyfin/plugins/xThemeSong`
   - **Docker**: `/config/plugins/xThemeSong`
4. Restart Jellyfin.

## 📖 Usage

### Assigning a Theme Song

1. Navigate to a movie or TV show in Jellyfin.
2. Click the **"⋮" (three dots)** menu.
3. Select **"Assign Theme Song"**.
4. A modal dialog will open showing:
   - 🎧 Existing theme song audio player, if available
   - YouTube URL/Video ID input field
   - Drag-and-drop area for MP3 files
5. Choose one of the following:
   - Enter a YouTube video ID or URL
   - Upload an MP3 file (drag-and-drop or browse)
6. Click **"Save Theme Song"**.
7. Wait for the loading animation to complete.
8. A success message will appear when done.

### Scheduled Task

The plugin includes a scheduled task that processes theme songs:

1. Go to **Dashboard → Scheduled Tasks**.
2. Find **"xTheme Songs"**.
3. Click **▶ Play** to run immediately, or
4. Configure the schedule.

## 📁 File Structure

For each media item with a theme song, the plugin creates:

```
/path/to/movie/
├── movie.mp4
├── theme.mp3          # The theme song audio file
└── theme.json         # Metadata about the theme song
```

### theme.json Format

```json
{
  "YouTubeId": "dQw4w9WgXcQ",
  "YouTubeUrl": "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
  "Title": "Never Gonna Give You Up",
  "Uploader": "RickAstleyVEVO",
  "DateAdded": "2025-01-04T12:00:00Z",
  "DateModified": "2025-01-04T12:00:00Z",
  "IsUserUploaded": false,
  "OriginalFileName": null
}
```

## ⚙️ Configuration

Access plugin settings in **Dashboard → Plugins → xThemeSong**.

### Settings Tab
- **Overwrite Existing Files**: Whether to overwrite existing `theme.mp3` files
- **Audio Bitrate**: Audio quality for downloaded theme songs
- **FFmpeg Path**: Custom path to FFmpeg executable, or leave empty for auto-detect
- **Permission Mode**: Control who can manage theme songs (**Admins Only / Library Managers / Everyone**)

### Backup & Migration
- **Export to JSON**: Download all theme assignments for backup
- **Export to CSV**: Export for editing in spreadsheet applications
- **Import from JSON**: Restore themes from backup with conflict detection
- **Use Cases**: Server migrations, backups, and bulk management

### Media Management Tab
The Media Management tab provides an overview of your movies and TV shows:

- **Statistics**: See total media count, items with themes, and items without themes
- **Library Tables**: View movies and TV shows grouped by library
- **Theme Status**: Quick badges showing which items have theme songs
- **Mini Audio Player**: Preview existing theme songs directly in the table
- **YouTube URL Input**: Enter YouTube URLs for each item
- **Bulk Save**: Save URLs for multiple items, then run the scheduled task to download
- **Pagination**: Choose 25, 50, or 100 items per page and move between pages
- **Search and Filters**: Search by title and filter by media type, library, and theme status

Pagination limits how many rows are rendered at once, which improves responsiveness for large media libraries while retaining the existing search and filtering controls.

### User Preferences
Access from **Dashboard → Plugins → xThemeSong User Preferences**.

Each user can customize their theme song experience:
- **Enable/Disable Theme Songs**: Turn theme songs on or off for your account
- **Maximum Duration**: Limit playback to X seconds (0 = play full theme)
- **Volume Control**: Adjust theme song volume (0-100%)
- **Server-Side Storage**: Preferences sync across your devices

### Deleting Theme Songs

To remove an existing theme song:

1. Navigate to the movie or TV show.
2. Click the **"⋮" (three dots)** menu and select **"Assign Theme Song"**.
3. Click the **"🗑️ Delete"** button next to the existing theme.
4. Confirm the deletion.

## 🐛 Troubleshooting

### Plugin doesn't appear in Jellyfin

1. Check Jellyfin logs for errors: `/config/log/log_*.log`.
2. Ensure you're running **Jellyfin 12.0.0 or later**.
3. Verify the plugin files are in the correct directory.
4. Restart Jellyfin after installation.

### Theme songs not downloading

1. Check that FFmpeg is installed and accessible.
2. Verify that you have an internet connection.
3. Check the scheduled task logs in **Dashboard → Scheduled Tasks**.
4. Ensure the YouTube URL/ID is valid.

### Media Management is slow or does not show all items

- Use the built-in pagination controls to limit the number of rows rendered at once.
- Use search and the library/theme filters to narrow the displayed results.
- If you upgraded from an older release, restart Jellyfin after installing the current release.

## 🔨 Build from Source

This fork targets Jellyfin 12 and .NET 10.

```bash
git clone https://github.com/ccook45/Jellyfin.Plugin.AssignThemeSong.git
cd Jellyfin.Plugin.AssignThemeSong
dotnet build -c Release
dotnet publish -c Release -o publish
```

The project currently uses **YoutubeExplode 6.6.2** for YouTube access.

## 📝 Development Status

**Current Version**: **v1.4.8**

### v1.4.8
- ✅ Fixed plugin version reporting so the installed assembly/package reports the release version correctly
- ✅ Explicitly packages the plugin logo so it is available in the release
- ✅ Retains the Media Management pagination and rendering improvements

### v1.4.7
- ✅ **Media Management Pagination** - Large libraries are displayed a page at a time instead of rendering every row at once
- ✅ **Lazy poster loading** - Poster images are loaded as needed
- ✅ **Deferred audio loading** - Audio elements use deferred loading to reduce initial page load work

### v1.4.6
- ✅ Updated **YoutubeExplode** to 6.6.2 for current YouTube compatibility
- ✅ Updated the plugin for Jellyfin 12 / .NET 10

### Jellyfin 12 Compatibility
- ✅ Targets **Jellyfin 12.0.0**
- ✅ Targets **.NET 10**
- ✅ Updated authorization handling for Jellyfin 12 role claims
- ✅ Updated plugin API usage for Jellyfin 12
- ✅ Release workflow builds and publishes Jellyfin 12-compatible packages

### Earlier Features
- ✅ **Season/Collection-Level Theme Inheritance** - Assign themes at Series, Season, or BoxSet level
- ✅ **Media Library Filters** - Filter by theme status and search by title
- ✅ **Poster Thumbnails** - Display movie/show artwork in library overview
- ✅ **Library Type Tabs** - Quick filter by Movies/Series or specific library
- ✅ **Export/Import Theme Mappings** - JSON & CSV export and import with conflict resolution
- ✅ **Role-Based Access Control** - Admins/Library Managers/Everyone permission modes
- ✅ **Per-User Theme Preferences** - Enable/disable, volume, duration control per user
- ✅ **User Preferences Page** - Accessible to all users for customization
- ✅ **Tabbed Settings Page** - Settings and Media Management tabs
- ✅ **Bulk YouTube URL Assignment** - Set URLs for multiple items and download via scheduled task
- ✅ **Statistics Dashboard** - Total media, with themes, without themes counts
- ✅ **Inline Audio Players** - Preview theme songs directly in the library table
- ✅ **Delete Theme Songs** with confirmation
- ✅ **Drag-and-drop** file upload
- ✅ **Custom FFmpeg Path** configuration
- ✅ **Cross-Platform FFmpeg Detection** - Windows, Mac, Linux, Docker
- ✅ **File Transformation Plugin Integration**

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- [Jellyfin](https://github.com/jellyfin/jellyfin) - The media server
- [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) - YouTube download library
- Reference plugins: File Transformation, HoverTrailer, and others

## 📧 Support

For issues and questions:
- [GitHub Issues](https://github.com/ccook45/Jellyfin.Plugin.AssignThemeSong/issues)
- [Jellyfin Forum](https://forum.jellyfin.org/)

---

**Note**: Please report any bugs or issues on GitHub.
