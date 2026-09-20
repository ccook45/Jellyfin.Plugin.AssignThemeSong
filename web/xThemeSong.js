(function() {
    'use strict';
    
    console.log('xThemeSong: Dialog module loaded');
    
    // CSS for dialogs and loading
    var styleElement = document.createElement('style');
    styleElement.textContent = `
        .xthemesong-overlay {
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(0, 0, 0, 0.85);
            z-index: 10000;
            display: flex;
            align-items: center;
            justify-content: center;
        }
        .xthemesong-dialog {
            background: #1e1e1e;
            padding: 24px;
            border-radius: 12px;
            max-width: 500px;
            width: 90%;
            max-height: 85vh;
            overflow-y: auto;
            box-shadow: 0 8px 32px rgba(0,0,0,0.5);
        }
        .xthemesong-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 20px;
            padding-bottom: 12px;
            border-bottom: 1px solid #333;
        }
        .xthemesong-title {
            margin: 0;
            color: #fff;
            font-size: 1.4em;
        }
        .xthemesong-close {
            background: none;
            border: none;
            color: #888;
            font-size: 24px;
            cursor: pointer;
            padding: 4px 8px;
            border-radius: 4px;
        }
        .xthemesong-close:hover {
            background: #333;
            color: #fff;
        }
        .xthemesong-section {
            margin-bottom: 20px;
        }
        .xthemesong-label {
            color: #aaa;
            display: block;
            margin-bottom: 8px;
            font-size: 0.9em;
        }
        .xthemesong-input {
            width: 100%;
            padding: 12px;
            border-radius: 6px;
            border: 1px solid #444;
            background: #2a2a2a;
            color: #fff;
            font-size: 14px;
            box-sizing: border-box;
        }
        .xthemesong-input:focus {
            outline: none;
            border-color: #00a4dc;
        }
        .xthemesong-dropzone {
            border: 2px dashed #444;
            border-radius: 8px;
            padding: 30px;
            text-align: center;
            cursor: pointer;
            transition: all 0.3s;
        }
        .xthemesong-dropzone:hover, .xthemesong-dropzone.active {
            border-color: #00a4dc;
            background: rgba(0, 164, 220, 0.1);
        }
        .xthemesong-dropzone-icon {
            font-size: 48px;
            color: #555;
            margin-bottom: 12px;
        }
        .xthemesong-dropzone-text {
            color: #888;
            margin: 8px 0;
        }
        .xthemesong-file-info {
            color: #00a4dc;
            margin-top: 12px;
            font-size: 0.9em;
        }
        .xthemesong-btn {
            padding: 12px 24px;
            border: none;
            border-radius: 6px;
            cursor: pointer;
            font-size: 14px;
            font-weight: 500;
            transition: all 0.2s;
        }
        .xthemesong-btn-primary {
            background: #00a4dc;
            color: #fff;
        }
        .xthemesong-btn-primary:hover {
            background: #0090c4;
        }
        .xthemesong-btn-primary:disabled {
            background: #555;
            cursor: not-allowed;
        }
        .xthemesong-btn-secondary {
            background: #444;
            color: #fff;
        }
        .xthemesong-btn-secondary:hover {
            background: #555;
        }
        .xthemesong-footer {
            display: flex;
            gap: 12px;
            justify-content: flex-end;
            margin-top: 24px;
            padding-top: 16px;
            border-top: 1px solid #333;
        }
        .xthemesong-player {
            width: 100%;
            margin-top: 12px;
            border-radius: 6px;
        }
        .xthemesong-loading {
            display: flex;
            flex-direction: column;
            align-items: center;
            padding: 40px;
        }
        .xthemesong-spinner {
            width: 50px;
            height: 50px;
            border: 4px solid #333;
            border-top-color: #00a4dc;
            border-radius: 50%;
            animation: xthemesong-spin 1s linear infinite;
        }
        @keyframes xthemesong-spin {
            to { transform: rotate(360deg); }
        }
        .xthemesong-loading-text {
            color: #aaa;
            margin-top: 16px;
            font-size: 14px;
        }
        .xthemesong-message {
            text-align: center;
            padding: 20px;
        }
        .xthemesong-message-icon {
            font-size: 48px;
            margin-bottom: 16px;
        }
        .xthemesong-message-icon.success { color: #4caf50; }
        .xthemesong-message-icon.error { color: #f44336; }
        .xthemesong-message-text {
            color: #fff;
            font-size: 16px;
            margin-bottom: 8px;
        }
        .xthemesong-message-detail {
            color: #888;
            font-size: 14px;
        }
        .xthemesong-existing {
            background: #252525;
            border-radius: 8px;
            padding: 16px;
        }
        .xthemesong-existing-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 8px;
        }
        .xthemesong-existing-title {
            color: #fff;
            margin: 0;
            font-size: 1em;
        }
        .xthemesong-existing-meta {
            color: #888;
            font-size: 0.85em;
            margin-top: 8px;
        }
        .xthemesong-btn-delete {
            background: #dc3545;
            color: #fff;
            padding: 6px 12px;
            font-size: 12px;
        }
        .xthemesong-btn-delete:hover {
            background: #c82333;
        }
        .xthemesong-confirm-dialog {
            text-align: center;
            padding: 20px;
        }
        .xthemesong-confirm-icon {
            font-size: 48px;
            color: #ffc107;
            margin-bottom: 16px;
        }
        .xthemesong-confirm-text {
            color: #fff;
            font-size: 16px;
            margin-bottom: 8px;
        }
        .xthemesong-confirm-buttons {
            display: flex;
            gap: 12px;
            justify-content: center;
            margin-top: 20px;
        }
        .xthemesong-select {
            width: 100%;
            padding: 12px;
            border-radius: 6px;
            border: 1px solid #444;
            background: #2a2a2a;
            color: #fff;
            font-size: 14px;
            box-sizing: border-box;
        }
        .xthemesong-select:focus {
            outline: none;
            border-color: #00a4dc;
        }
        .xthemesong-search-results {
            margin-top: 12px;
            display: flex;
            flex-direction: column;
            gap: 8px;
        }
        .xthemesong-search-result {
            background: #252525;
            border: 1px solid #333;
            border-radius: 6px;
            padding: 10px;
            display: flex;
            gap: 10px;
            align-items: center;
        }
        .xthemesong-search-result-thumb {
            width: 120px;
            height: 68px;
            object-fit: cover;
            border-radius: 4px;
            background: #111;
            flex-shrink: 0;
        }
        .xthemesong-search-result-info {
            flex: 1;
            min-width: 0;
        }
        .xthemesong-search-result-title {
            color: #fff;
            font-size: 13px;
            font-weight: 500;
            overflow-wrap: anywhere;
        }
        .xthemesong-search-result-meta {
            color: #888;
            font-size: 11px;
            margin-top: 4px;
        }
        .xthemesong-search-result-link {
            color: #00a4dc;
            background: none;
            border: 0;
            cursor: pointer;
            font-size: 11px;
            text-decoration: none;
            white-space: nowrap;
            flex-shrink: 0;
        }
        .xthemesong-search-result-link:hover {
            text-decoration: underline;
        }
        .xthemesong-search-result button {
            flex-shrink: 0;
            padding: 7px 10px;
            font-size: 12px;
        }
        .xthemesong-inherited {
            background: #1a2a1a;
            border: 1px solid #2a4a2a;
            border-radius: 8px;
            padding: 12px;
            margin-top: 8px;
            color: #8f8;
            font-size: 0.85em;
        }
    `;
    document.head.appendChild(styleElement);
    
    function showThemeSongDialog(itemId) {
        console.log('xThemeSong: Opening dialog for item', itemId);
        closeActionSheets();
        createDialog(itemId);
    }
    
    function closeActionSheets() {
        var actionSheets = document.querySelectorAll('.actionSheet, .dialogContainer, .dialog-container');
        actionSheets.forEach(function(el) {
            el.style.display = 'none';
            if (el.parentNode) el.parentNode.removeChild(el);
        });
        var backdrops = document.querySelectorAll('.backdrop, .backdropFadeIn');
        backdrops.forEach(function(el) {
            if (el.parentNode) el.parentNode.removeChild(el);
        });
    }
    
    function createDialog(itemId) {
        var overlay = document.createElement('div');
        overlay.className = 'xthemesong-overlay';
        
        var dialog = document.createElement('div');
        dialog.className = 'xthemesong-dialog';
        dialog.innerHTML = getDialogHTML();
        
        overlay.appendChild(dialog);
        document.body.appendChild(overlay);
        
        // Load existing theme
        loadExistingTheme(itemId, dialog);
        
        // Setup event handlers
        setupDialogEvents(overlay, dialog, itemId);
    }
    
    function getDialogHTML() {
        return `
            <div class="xthemesong-header">
                <h2 class="xthemesong-title">🎵 xThemeSong</h2>
                <button class="xthemesong-close" id="xthemesongClose">&times;</button>
            </div>
            
            <div id="xthemesongContent">
                <div id="xthemesongExisting" class="xthemesong-section" style="display:none;">
                    <div class="xthemesong-existing">
                        <div class="xthemesong-existing-header">
                            <h4 class="xthemesong-existing-title">🎧 Current Theme Song</h4>
                            <button id="xthemesongDelete" class="xthemesong-btn xthemesong-btn-delete">🗑️ Delete</button>
                        </div>
                        <audio id="xthemesongPlayer" class="xthemesong-player" controls></audio>
                        <div id="xthemesongMeta" class="xthemesong-existing-meta"></div>
                    </div>
                </div>
                
                <div id="xthemesongInherited" class="xthemesong-section" style="display:none;">
                    <div class="xthemesong-existing" style="background:#1a2a1a;border:1px solid #2a4a2a;">
                        <h4 class="xthemesong-existing-title" style="color:#4caf50;">🔗 Inherited Theme Song</h4>
                        <audio id="xthemesongInheritedPlayer" class="xthemesong-player" controls></audio>
                        <div id="xthemesongInheritedMeta" class="xthemesong-existing-meta"></div>
                    </div>
                </div>
                
                <div class="xthemesong-section">
                    <label class="xthemesong-label">Assign Theme To</label>
                    <select id="xthemesongTargetType" class="xthemesong-input" style="cursor:pointer;">
                        <option value="auto">Auto-detect (This Item)</option>
                        <option value="Movie">This Movie</option>
                        <option value="Series">This Series</option>
                        <option value="Season">This Season</option>
                        <option value="BoxSet">This Collection</option>
                    </select>
                </div>
                
                <div class="xthemesong-section">
                    <label class="xthemesong-label">YouTube Theme Song</label>
                    <div style="display:flex;gap:8px;">
                        <input type="text" id="xthemesongYouTube" class="xthemesong-input"
                               placeholder="Paste a YouTube URL or video ID">
                        <button type="button" id="xthemesongSearch" class="xthemesong-btn xthemesong-btn-secondary" style="white-space:nowrap;">🔎 Auto Search</button>
                    </div>
                    <div style="color:#777;font-size:11px;margin-top:6px;">
                        Searches YouTube using the media title, type, and optional release year, then ranks short theme-song matches.
                    </div>
                    <div id="xthemesongSearchResults" class="xthemesong-search-results" style="display:none;"></div>
                </div>
                
                <div class="xthemesong-section">
                    <label class="xthemesong-label">Or Upload MP3 File</label>
                    <div id="xthemesongDropzone" class="xthemesong-dropzone">
                        <div class="xthemesong-dropzone-icon">📁</div>
                        <div class="xthemesong-dropzone-text">Drag & drop an MP3 file here</div>
                        <div class="xthemesong-dropzone-text" style="font-size:0.9em;">or click to browse</div>
                        <input type="file" id="xthemesongFile" accept=".mp3,audio/mpeg" style="display:none;">
                    </div>
                    <div id="xthemesongFileInfo" class="xthemesong-file-info" style="display:none;"></div>
                </div>
                
                <div class="xthemesong-footer">
                    <button id="xthemesongCancel" class="xthemesong-btn xthemesong-btn-secondary">Cancel</button>
                    <button id="xthemesongSubmit" class="xthemesong-btn xthemesong-btn-primary">Save Theme Song</button>
                </div>
            </div>
        `;
    }
    
    function showLoading(dialog, message) {
        var content = dialog.querySelector('#xthemesongContent');
        content.innerHTML = `
            <div class="xthemesong-loading">
                <div class="xthemesong-spinner"></div>
                <div class="xthemesong-loading-text">${message || 'Processing...'}</div>
            </div>
        `;
    }
    
    function showMessage(dialog, type, title, detail, overlay) {
        var icon = type === 'success' ? '✅' : '❌';
        var content = dialog.querySelector('#xthemesongContent');
        content.innerHTML = `
            <div class="xthemesong-message">
                <div class="xthemesong-message-icon ${type}">${icon}</div>
                <div class="xthemesong-message-text">${title}</div>
                <div class="xthemesong-message-detail">${detail || ''}</div>
            </div>
            <div class="xthemesong-footer">
                <button id="xthemesongOk" class="xthemesong-btn xthemesong-btn-primary">OK</button>
            </div>
        `;
        
        dialog.querySelector('#xthemesongOk').addEventListener('click', function() {
            document.body.removeChild(overlay);
        });
    }
    
    function setupDialogEvents(overlay, dialog, itemId) {
        var selectedFile = null;
        
        // Close button
        dialog.querySelector('#xthemesongClose').addEventListener('click', function() {
            document.body.removeChild(overlay);
        });
        
        // Cancel button
        dialog.querySelector('#xthemesongCancel').addEventListener('click', function() {
            document.body.removeChild(overlay);
        });
        
        // Click outside to close
        overlay.addEventListener('click', function(e) {
            if (e.target === overlay) {
                document.body.removeChild(overlay);
            }
        });
        
        // Dropzone events
        var dropzone = dialog.querySelector('#xthemesongDropzone');
        var fileInput = dialog.querySelector('#xthemesongFile');
        var fileInfo = dialog.querySelector('#xthemesongFileInfo');
        
        dropzone.addEventListener('click', function() {
            fileInput.click();
        });
        
        ['dragenter', 'dragover'].forEach(function(e) {
            dropzone.addEventListener(e, function(ev) {
                ev.preventDefault();
                dropzone.classList.add('active');
            });
        });
        
        ['dragleave', 'drop'].forEach(function(e) {
            dropzone.addEventListener(e, function(ev) {
                ev.preventDefault();
                dropzone.classList.remove('active');
            });
        });
        
        dropzone.addEventListener('drop', function(e) {
            e.preventDefault();
            var files = e.dataTransfer.files;
            if (files.length > 0 && (files[0].type === 'audio/mpeg' || files[0].name.endsWith('.mp3'))) {
                selectedFile = files[0];
                showSelectedFile(selectedFile, fileInfo);
            }
        });
        
        fileInput.addEventListener('change', function() {
            if (this.files.length > 0) {
                selectedFile = this.files[0];
                showSelectedFile(selectedFile, fileInfo);
            }
        });
        
        // Auto-search YouTube for this media item
        var searchBtn = dialog.querySelector('#xthemesongSearch');
        if (searchBtn) {
            searchBtn.addEventListener('click', function() {
                searchYouTubeThemes(itemId, dialog);
            });
        }

        // Submit button
        dialog.querySelector('#xthemesongSubmit').addEventListener('click', function() {
            var youtubeUrl = dialog.querySelector('#xthemesongYouTube').value.trim();
            var targetType = dialog.querySelector('#xthemesongTargetType').value;
            
            if (!youtubeUrl && !selectedFile) {
                showMessage(dialog, 'error', 'Missing Input', 'Please enter a YouTube URL or upload an MP3 file.', overlay);
                return;
            }
            
            // Show loading
            showLoading(dialog, youtubeUrl ? 'Downloading from YouTube...' : 'Uploading file...');
            
            // Prepare form data
            var formData = new FormData();
            if (youtubeUrl) formData.append('YouTubeUrl', youtubeUrl);
            if (selectedFile) formData.append('UploadedFile', selectedFile);
            if (targetType && targetType !== 'auto') formData.append('TargetType', targetType);
            
            // API call
            var apiUrl = ApiClient.getUrl('xThemeSong/' + itemId);
            
            fetch(apiUrl, {
                method: 'POST',
                headers: { 'Authorization': 'MediaBrowser Client="xThemeSong", Device="Web", DeviceId="xThemeSong", Version="1.4.1", Token="' + ApiClient.accessToken() + '"' },
                body: formData
            }).then(function(response) {
                if (response.ok) {
                    showMessage(dialog, 'success', 'Theme Song Saved!', 'The theme song has been successfully assigned.', overlay);
                } else {
                    return response.text().then(function(text) {
                        throw new Error(text || 'Failed to assign theme song');
                    });
                }
            }).catch(function(error) {
                showMessage(dialog, 'error', 'Error', error.message || 'Failed to assign theme song', overlay);
            });
        });
    }
    
    function escapeHtml(value) {
        var div = document.createElement('div');
        div.textContent = value == null ? '' : String(value);
        return div.innerHTML;
    }

    function formatSearchDuration(duration) {
        if (!duration) return 'Unknown duration';

        // System.Text.Json serializes TimeSpan as a string such as "00:03:42".
        if (typeof duration === 'number') {
            if (!isFinite(duration) || duration <= 0) return 'Unknown duration';
            var totalSeconds = Math.round(duration);
            var minutes = Math.floor(totalSeconds / 60);
            var seconds = totalSeconds % 60;
            var hours = Math.floor(minutes / 60);
            minutes = minutes % 60;
            return hours > 0 ? hours + ':' + String(minutes).padStart(2, '0') + ':' + String(seconds).padStart(2, '0') : minutes + ':' + String(seconds).padStart(2, '0');
        }

        if (typeof duration === 'string') {
            var parts = duration.split(':').map(Number);
            if (parts.length === 3 && parts.every(function(part) { return !isNaN(part); })) {
                var hours = parts[0];
                var minutes = parts[1];
                var seconds = parts[2];
                return (hours > 0 ? hours + ':' : '') +
                    String(minutes + (hours > 0 ? 0 : 0)).padStart(hours > 0 ? 2 : 1, '0') +
                    ':' + String(seconds).padStart(2, '0');
            }
            if (parts.length === 2 && parts.every(function(part) { return !isNaN(part); })) {
                return parts[0] + ':' + String(parts[1]).padStart(2, '0');
            }
            return duration;
        }

        if (typeof duration.totalSeconds === 'number') {
            var totalSeconds = Math.round(duration.totalSeconds);
            var minutes = Math.floor(totalSeconds / 60);
            var seconds = totalSeconds % 60;
            return minutes + ':' + String(seconds).padStart(2, '0');
        }

        return 'Unknown duration';
    }

    function searchYouTubeThemes(itemId, dialog) {
        var searchBtn = dialog.querySelector('#xthemesongSearch');
        var resultsDiv = dialog.querySelector('#xthemesongSearchResults');
        if (!searchBtn || !resultsDiv) return;

        searchBtn.disabled = true;
        searchBtn.textContent = 'Searching...';
        resultsDiv.style.display = 'block';
        resultsDiv.innerHTML = '<div style="color:#aaa;padding:8px;">Searching YouTube for likely theme songs...</div>';

        var apiUrl = ApiClient.getUrl('xThemeSong/' + itemId + '/search');
        fetch(apiUrl, {
            headers: {
                'Authorization': 'MediaBrowser Client="xThemeSong", Device="Web", DeviceId="xThemeSong", Version="1.4.12", Token="' + ApiClient.accessToken() + '"'
            }
        }).then(function(response) {
            if (!response.ok) {
                return response.text().then(function(text) {
                    throw new Error(text || 'YouTube search failed');
                });
            }
            return response.json();
        }).then(function(results) {
            searchBtn.disabled = false;
            searchBtn.textContent = '🔎 Auto Search';

            if (!results || results.length === 0) {
                resultsDiv.innerHTML = '<div style="color:#aaa;padding:8px;">No likely YouTube theme songs were found.</div>';
                return;
            }

            resultsDiv.innerHTML = results.map(function(result, index) {
                return '<div class="xthemesong-search-result">' +
                    '<img class="xthemesong-search-result-thumb" src="' + escapeHtml(ApiClient.getUrl('xThemeSong/' + itemId + '/search/thumbnail?videoId=' + encodeURIComponent(result.videoId) + '&api_key=' + encodeURIComponent(ApiClient.accessToken()))) + '" alt="" loading="lazy">' +
                    '<div class="xthemesong-search-result-info">' +
                    '<div class="xthemesong-search-result-title">' + escapeHtml(result.title) + '</div>' +
                    '<div class="xthemesong-search-result-meta">' +
                    escapeHtml(result.channel || 'Unknown channel') + ' • ' +
                    formatSearchDuration(result.durationSeconds) +
                    (index === 0 ? ' • Suggested match' : '') +
                    '</div>' +
                    '<button type="button" class="xthemesong-search-result-link" data-youtube-url="' + escapeHtml(result.url) + '">▶ Verify on YouTube</button>' +
                    '</div>' +
                    '<button type="button" class="xthemesong-btn xthemesong-btn-primary" data-video-id="' + escapeHtml(result.videoId) + '" data-video-title="' + escapeHtml(result.title) + '">Download</button>' +
                    '</div>';
            }).join('');

            resultsDiv.querySelectorAll('.xthemesong-search-result-link[data-youtube-url]').forEach(function(button) {
                button.addEventListener('click', function() {
                    var url = this.getAttribute('data-youtube-url');
                    if (url) window.open(url, '_blank', 'noopener,noreferrer');
                });
            });

            resultsDiv.querySelectorAll('button[data-video-id]').forEach(function(button) {
                button.addEventListener('click', function() {
                    downloadYouTubeSearchResult(itemId, dialog, this.getAttribute('data-video-id'), this.getAttribute('data-video-title'));
                });
            });
        }).catch(function(error) {
            searchBtn.disabled = false;
            searchBtn.textContent = '🔎 Auto Search';
            resultsDiv.innerHTML = '<div style="color:#f44336;padding:8px;">Search failed: ' + escapeHtml(error.message || 'Unknown error') + '</div>';
        });
    }

    function downloadYouTubeSearchResult(itemId, dialog, videoId, videoTitle) {
        var targetType = dialog.querySelector('#xthemesongTargetType')?.value || 'auto';
        showLoading(dialog, 'Downloading "' + videoTitle + '" from YouTube...');

        var apiUrl = ApiClient.getUrl('xThemeSong/' + itemId + '/search/download');
        fetch(apiUrl, {
            method: 'POST',
            headers: {
                'Authorization': 'MediaBrowser Client="xThemeSong", Device="Web", DeviceId="xThemeSong", Version="1.4.10", Token="' + ApiClient.accessToken() + '"',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ videoId: videoId, targetType: targetType })
        }).then(function(response) {
            if (!response.ok) {
                return response.text().then(function(text) {
                    throw new Error(text || 'Failed to download selected theme');
                });
            }
            return response.json();
        }).then(function(result) {
            showMessage(
                dialog,
                'success',
                'Theme Song Saved!',
                (result.title || videoTitle) + ' has been downloaded and assigned.',
                dialog.closest('.xthemesong-overlay')
            );
        }).catch(function(error) {
            showMessage(
                dialog,
                'error',
                'Download Failed',
                error.message || 'Failed to download the selected theme song.',
                dialog.closest('.xthemesong-overlay')
            );
        });
    }

    function showSelectedFile(file, fileInfoElement) {
        var sizeInMB = (file.size / 1024 / 1024).toFixed(2);
        fileInfoElement.textContent = '📎 Selected: ' + file.name + ' (' + sizeInMB + ' MB)';
        fileInfoElement.style.display = 'block';
    }
    
    function loadExistingTheme(itemId, dialog) {
        // Fetch both direct theme metadata and hierarchy info in parallel
        var themeJsonUrl = ApiClient.getUrl('xThemeSong/' + itemId + '/metadata');
        var hierarchyUrl = ApiClient.getUrl('xThemeSong/' + itemId + '/hierarchy');
        
        var metaPromise = fetch(themeJsonUrl, {
            headers: { 'Authorization': 'MediaBrowser Client="xThemeSong", Device="Web", DeviceId="xThemeSong", Version="1.4.1", Token="' + ApiClient.accessToken() + '"' }
        }).then(function(response) {
            if (response.ok) return response.json();
            throw new Error('No theme');
        }).catch(function() { return null; });
        
        var hierarchyPromise = fetch(hierarchyUrl, {
            headers: { 'Authorization': 'MediaBrowser Client="xThemeSong", Device="Web", DeviceId="xThemeSong", Version="1.4.1", Token="' + ApiClient.accessToken() + '"' }
        }).then(function(response) {
            if (response.ok) return response.json();
            throw new Error('No hierarchy');
        }).catch(function() { return null; });
        
        Promise.all([metaPromise, hierarchyPromise]).then(function(results) {
            var metadata = results[0];
            var hierarchy = results[1];
            
            // Show direct theme if it exists
            if (metadata) {
                var existingSection = dialog.querySelector('#xthemesongExisting');
                var player = dialog.querySelector('#xthemesongPlayer');
                var metaDiv = dialog.querySelector('#xthemesongMeta');
                var deleteBtn = dialog.querySelector('#xthemesongDelete');
                
                var audioUrl = ApiClient.getUrl('xThemeSong/' + itemId + '/audio');
                player.src = audioUrl + '?api_key=' + ApiClient.accessToken();
                
                var metaText = [];
                if (metadata.Title) metaText.push('Title: ' + metadata.Title);
                if (metadata.Uploader) metaText.push('By: ' + metadata.Uploader);
                if (metadata.DateAdded) metaText.push('Added: ' + new Date(metadata.DateAdded).toLocaleDateString());
                if (metadata.TargetType && metadata.TargetType !== 'Movie') metaText.push('Target: ' + metadata.TargetType);
                metaDiv.textContent = metaText.join(' • ');
                
                existingSection.style.display = 'block';
                
                if (deleteBtn) {
                    deleteBtn.addEventListener('click', function() {
                        showDeleteConfirmation(itemId, dialog);
                    });
                }
            }
            
            // Show inherited theme if it exists and no direct theme
            if (hierarchy && hierarchy.HasInheritedTheme && !metadata) {
                var inheritedSection = dialog.querySelector('#xthemesongInherited');
                var inheritedPlayer = dialog.querySelector('#xthemesongInheritedPlayer');
                var inheritedMeta = dialog.querySelector('#xthemesongInheritedMeta');
                
                if (inheritedSection && inheritedPlayer && inheritedMeta) {
                    // Use the inherited item's audio endpoint
                    var inheritedAudioUrl = ApiClient.getUrl('xThemeSong/' + hierarchy.InheritedFromItemId + '/audio');
                    inheritedPlayer.src = inheritedAudioUrl + '?api_key=' + ApiClient.accessToken();
                    inheritedMeta.textContent = 'Inherited from: ' + hierarchy.InheritedFromItemName + ' (' + hierarchy.ItemType + ')';
                    inheritedSection.style.display = 'block';
                }
            }
        });
    }
    
    function showDeleteConfirmation(itemId, dialog) {
        var content = dialog.querySelector('#xthemesongContent');
        content.innerHTML = `
            <div class="xthemesong-confirm-dialog">
                <div class="xthemesong-confirm-icon">⚠️</div>
                <div class="xthemesong-confirm-text">Are you sure you want to delete this theme song?</div>
                <div class="xthemesong-message-detail">This action cannot be undone.</div>
                <div class="xthemesong-confirm-buttons">
                    <button id="xthemesongCancelDelete" class="xthemesong-btn xthemesong-btn-secondary">Cancel</button>
                    <button id="xthemesongConfirmDelete" class="xthemesong-btn xthemesong-btn-delete">🗑️ Delete</button>
                </div>
            </div>
        `;
        
        // Cancel - reload dialog
        dialog.querySelector('#xthemesongCancelDelete').addEventListener('click', function() {
            // Recreate dialog content
            content.innerHTML = getDialogContent();
            loadExistingTheme(itemId, dialog);
            setupFormEvents(dialog, itemId);
        });
        
        // Confirm delete
        dialog.querySelector('#xthemesongConfirmDelete').addEventListener('click', function() {
            deleteThemeSong(itemId, dialog);
        });
    }
    
    function deleteThemeSong(itemId, dialog) {
        var content = dialog.querySelector('#xthemesongContent');
        content.innerHTML = `
            <div class="xthemesong-loading">
                <div class="xthemesong-spinner"></div>
                <div class="xthemesong-loading-text">Deleting theme song...</div>
            </div>
        `;
        
        var apiUrl = ApiClient.getUrl('xThemeSong/' + itemId);
        
        fetch(apiUrl, {
            method: 'DELETE',
            headers: { 'Authorization': 'MediaBrowser Client="xThemeSong", Device="Web", DeviceId="xThemeSong", Version="1.4.1", Token="' + ApiClient.accessToken() + '"' }
        }).then(function(response) {
            if (response.ok) {
                return response.json();
            }
            return response.text().then(function(text) {
                throw new Error(text || 'Failed to delete theme song');
            });
        }).then(function(result) {
            content.innerHTML = `
                <div class="xthemesong-message">
                    <div class="xthemesong-message-icon success">✅</div>
                    <div class="xthemesong-message-text">Theme Song Deleted!</div>
                    <div class="xthemesong-message-detail">${result.message || 'The theme song has been removed.'}</div>
                </div>
                <div class="xthemesong-footer">
                    <button id="xthemesongOkDelete" class="xthemesong-btn xthemesong-btn-primary">OK</button>
                </div>
            `;
            dialog.querySelector('#xthemesongOkDelete').addEventListener('click', function() {
                var overlay = dialog.closest('.xthemesong-overlay');
                if (overlay) document.body.removeChild(overlay);
            });
        }).catch(function(error) {
            content.innerHTML = `
                <div class="xthemesong-message">
                    <div class="xthemesong-message-icon error">❌</div>
                    <div class="xthemesong-message-text">Delete Failed</div>
                    <div class="xthemesong-message-detail">${error.message || 'Failed to delete theme song'}</div>
                </div>
                <div class="xthemesong-footer">
                    <button id="xthemesongOkDelete" class="xthemesong-btn xthemesong-btn-primary">OK</button>
                </div>
            `;
            dialog.querySelector('#xthemesongOkDelete').addEventListener('click', function() {
                var overlay = dialog.closest('.xthemesong-overlay');
                if (overlay) document.body.removeChild(overlay);
            });
        });
    }
    
    function getDialogContent() {
        return `
            <div id="xthemesongExisting" class="xthemesong-section" style="display:none;">
                <div class="xthemesong-existing">
                    <div class="xthemesong-existing-header">
                        <h4 class="xthemesong-existing-title">🎧 Current Theme Song</h4>
                        <button id="xthemesongDelete" class="xthemesong-btn xthemesong-btn-delete">🗑️ Delete</button>
                    </div>
                    <audio id="xthemesongPlayer" class="xthemesong-player" controls></audio>
                    <div id="xthemesongMeta" class="xthemesong-existing-meta"></div>
                </div>
            </div>
            
            <div class="xthemesong-section">
                <label class="xthemesong-label">YouTube URL or Video ID</label>
                <input type="text" id="xthemesongYouTube" class="xthemesong-input" 
                       placeholder="https://www.youtube.com/watch?v=... or video ID">
            </div>
            
            <div class="xthemesong-section">
                <label class="xthemesong-label">Or Upload MP3 File</label>
                <div id="xthemesongDropzone" class="xthemesong-dropzone">
                    <div class="xthemesong-dropzone-icon">📁</div>
                    <div class="xthemesong-dropzone-text">Drag & drop an MP3 file here</div>
                    <div class="xthemesong-dropzone-text" style="font-size:0.9em;">or click to browse</div>
                    <input type="file" id="xthemesongFile" accept=".mp3,audio/mpeg" style="display:none;">
                </div>
                <div id="xthemesongFileInfo" class="xthemesong-file-info" style="display:none;"></div>
            </div>
            
            <div class="xthemesong-footer">
                <button id="xthemesongCancel" class="xthemesong-btn xthemesong-btn-secondary">Cancel</button>
                <button id="xthemesongSubmit" class="xthemesong-btn xthemesong-btn-primary">Save Theme Song</button>
            </div>
        `;
    }
    
    function setupFormEvents(dialog, itemId) {
        var selectedFile = null;
        var overlay = dialog.closest('.xthemesong-overlay');
        
        // Cancel button
        var cancelBtn = dialog.querySelector('#xthemesongCancel');
        if (cancelBtn) {
            cancelBtn.addEventListener('click', function() {
                if (overlay) document.body.removeChild(overlay);
            });
        }
        
        // Dropzone events
        var dropzone = dialog.querySelector('#xthemesongDropzone');
        var fileInput = dialog.querySelector('#xthemesongFile');
        var fileInfo = dialog.querySelector('#xthemesongFileInfo');
        
        if (dropzone && fileInput) {
            dropzone.addEventListener('click', function() {
                fileInput.click();
            });
            
            ['dragenter', 'dragover'].forEach(function(e) {
                dropzone.addEventListener(e, function(ev) {
                    ev.preventDefault();
                    dropzone.classList.add('active');
                });
            });
            
            ['dragleave', 'drop'].forEach(function(e) {
                dropzone.addEventListener(e, function(ev) {
                    ev.preventDefault();
                    dropzone.classList.remove('active');
                });
            });
            
            dropzone.addEventListener('drop', function(e) {
                e.preventDefault();
                var files = e.dataTransfer.files;
                if (files.length > 0 && (files[0].type === 'audio/mpeg' || files[0].name.endsWith('.mp3'))) {
                    selectedFile = files[0];
                    showSelectedFile(selectedFile, fileInfo);
                }
            });
            
            fileInput.addEventListener('change', function() {
                if (this.files.length > 0) {
                    selectedFile = this.files[0];
                    showSelectedFile(selectedFile, fileInfo);
                }
            });
        }
        
        // Submit button
        var submitBtn = dialog.querySelector('#xthemesongSubmit');
        if (submitBtn) {
            submitBtn.addEventListener('click', function() {
                var youtubeUrl = dialog.querySelector('#xthemesongYouTube').value.trim();
                
                if (!youtubeUrl && !selectedFile) {
                    showMessage(dialog, 'error', 'Missing Input', 'Please enter a YouTube URL or upload an MP3 file.', overlay);
                    return;
                }
                
                showLoading(dialog, youtubeUrl ? 'Downloading from YouTube...' : 'Uploading file...');
                
                var formData = new FormData();
                if (youtubeUrl) formData.append('YouTubeUrl', youtubeUrl);
                if (selectedFile) formData.append('UploadedFile', selectedFile);
                
                var apiUrl = ApiClient.getUrl('xThemeSong/' + itemId);
                
                fetch(apiUrl, {
                    method: 'POST',
                    headers: { 'Authorization': 'MediaBrowser Client="xThemeSong", Device="Web", DeviceId="xThemeSong", Version="1.4.1", Token="' + ApiClient.accessToken() + '"' },
                    body: formData
                }).then(function(response) {
                    if (response.ok) {
                        showMessage(dialog, 'success', 'Theme Song Saved!', 'The theme song has been successfully assigned.', overlay);
                    } else {
                        return response.text().then(function(text) {
                            throw new Error(text || 'Failed to assign theme song');
                        });
                    }
                }).catch(function(error) {
                    showMessage(dialog, 'error', 'Error', error.message || 'Failed to assign theme song', overlay);
                });
            });
        }
    }
    
    // Export to global scope
    window.xThemeSongDialog = {
        show: showThemeSongDialog
    };
    
    console.log('xThemeSong: Dialog exported to window.xThemeSongDialog');
})();
