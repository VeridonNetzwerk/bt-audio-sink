using System.Diagnostics;
using Windows.Media.Control;

namespace BtAudioSink.Media;

/// <summary>
/// Event arguments for media property changes (title, artist, etc.).
/// </summary>
public sealed class MediaMetadataChangedEventArgs : EventArgs
{
    public string? Title { get; init; }
    public string? Artist { get; init; }
    public string? AlbumTitle { get; init; }
}

/// <summary>
/// Event arguments for playback status changes.
/// </summary>
public sealed class PlayerPlaybackStatusChangedEventArgs : EventArgs
{
    public bool IsPlaying { get; init; }
}

/// <summary>
/// Monitors and controls system media playback via Global System Media Transport Controls (GSMTC).
/// When a Bluetooth device streams audio, its media session appears here, enabling
/// bidirectional media control between the PC and the connected phone.
/// </summary>
public sealed class MediaControlService : IDisposable
{
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private bool _disposed;

    /// <summary>
    /// Fired when media properties (title, artist) change.
    /// </summary>
    public event EventHandler<MediaMetadataChangedEventArgs>? MediaPropertiesChanged;

    /// <summary>
    /// Fired when playback status (playing/paused) changes.
    /// </summary>
    public event EventHandler<PlayerPlaybackStatusChangedEventArgs>? PlaybackStatusChanged;

    /// <summary>
    /// Fired when a media session becomes available or disappears.
    /// </summary>
    public event EventHandler<bool>? SessionAvailabilityChanged;

    /// <summary>
    /// Current media title.
    /// </summary>
    public string? CurrentTitle { get; private set; }

    /// <summary>
    /// Current media artist.
    /// </summary>
    public string? CurrentArtist { get; private set; }

    /// <summary>
    /// Current album title.
    /// </summary>
    public string? CurrentAlbumTitle { get; private set; }

    /// <summary>
    /// Whether media is currently playing.
    /// </summary>
    public bool IsPlaying { get; private set; }

    /// <summary>
    /// Whether a media session is currently active.
    /// </summary>
    public bool HasSession => _currentSession != null;

    /// <summary>
    /// Initializes the media control service by requesting the GSMTC session manager.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;

            // Attach to the current session if one exists
            UpdateCurrentSession(_sessionManager.GetCurrentSession());

            Debug.WriteLine("MediaControlService: Initialized successfully.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MediaControlService: Initialization failed - {ex.Message}");
            // Media controls are optional; the app still works without them
        }
    }

    /// <summary>
    /// Sends a play command to the current media session.
    /// </summary>
    public async Task PlayAsync()
    {
        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TryPlayAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MediaControlService: Play failed - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Sends a pause command to the current media session.
    /// </summary>
    public async Task PauseAsync()
    {
        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TryPauseAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MediaControlService: Pause failed - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Toggles play/pause based on current state.
    /// </summary>
    public async Task PlayPauseAsync()
    {
        if (_currentSession != null)
        {
            try
            {
                if (IsPlaying)
                {
                    await _currentSession.TryPauseAsync();
                }
                else
                {
                    await _currentSession.TryPlayAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MediaControlService: PlayPause failed - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Sends a skip-next command to the current media session.
    /// </summary>
    public async Task NextAsync()
    {
        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TrySkipNextAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MediaControlService: Next failed - {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Sends a skip-previous command to the current media session.
    /// </summary>
    public async Task PreviousAsync()
    {
        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TrySkipPreviousAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MediaControlService: Previous failed - {ex.Message}");
            }
        }
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        UpdateCurrentSession(sender.GetCurrentSession());
    }

    private void UpdateCurrentSession(GlobalSystemMediaTransportControlsSession? session)
    {
        // Detach from old session
        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
        }

        _currentSession = session;

        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;

            // Read current state
            _ = RefreshMediaPropertiesAsync();
            RefreshPlaybackInfo();

            Debug.WriteLine($"MediaControlService: Session attached - {_currentSession.SourceAppUserModelId}");
        }
        else
        {
            CurrentTitle = null;
            CurrentArtist = null;
            CurrentAlbumTitle = null;
            IsPlaying = false;

            MediaPropertiesChanged?.Invoke(this, new MediaMetadataChangedEventArgs());
            PlaybackStatusChanged?.Invoke(this, new PlayerPlaybackStatusChangedEventArgs { IsPlaying = false });
        }

        SessionAvailabilityChanged?.Invoke(this, _currentSession != null);
    }

    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, global::Windows.Media.Control.MediaPropertiesChangedEventArgs args)
    {
        _ = RefreshMediaPropertiesAsync();
    }

    private async Task RefreshMediaPropertiesAsync()
    {
        if (_currentSession == null)
        {
            return;
        }

        try
        {
            var props = await _currentSession.TryGetMediaPropertiesAsync();
            if (props != null)
            {
                CurrentTitle = string.IsNullOrWhiteSpace(props.Title) ? null : props.Title;
                CurrentArtist = string.IsNullOrWhiteSpace(props.Artist) ? null : props.Artist;
                CurrentAlbumTitle = string.IsNullOrWhiteSpace(props.AlbumTitle) ? null : props.AlbumTitle;

                MediaPropertiesChanged?.Invoke(this, new MediaMetadataChangedEventArgs
                {
                    Title = CurrentTitle,
                    Artist = CurrentArtist,
                    AlbumTitle = CurrentAlbumTitle
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MediaControlService: Failed to get media properties - {ex.Message}");
        }
    }

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, global::Windows.Media.Control.PlaybackInfoChangedEventArgs args)
    {
        RefreshPlaybackInfo();
    }

    private void RefreshPlaybackInfo()
    {
        if (_currentSession == null)
        {
            return;
        }

        try
        {
            var info = _currentSession.GetPlaybackInfo();
            if (info != null)
            {
                var wasPlaying = IsPlaying;
                IsPlaying = info.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

                if (wasPlaying != IsPlaying)
                {
                    PlaybackStatusChanged?.Invoke(this, new PlayerPlaybackStatusChangedEventArgs { IsPlaying = IsPlaying });
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MediaControlService: Failed to get playback info - {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            _currentSession = null;
        }

        if (_sessionManager != null)
        {
            _sessionManager.CurrentSessionChanged -= OnCurrentSessionChanged;
            _sessionManager = null;
        }
    }
}
