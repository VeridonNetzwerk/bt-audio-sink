using System.Diagnostics;
using Windows.Media.Control;

namespace BtAudioSink.Media;

public sealed class MediaStateChangedEventArgs : EventArgs
{
    public required bool HasActiveSession { get; init; }
    public required bool CanPlayPause { get; init; }
    public required bool CanSkipNext { get; init; }
    public required bool CanSkipPrevious { get; init; }
    public required bool IsPlaying { get; init; }
    public string? Title { get; init; }
    public string? Artist { get; init; }
}

/// <summary>
/// Provides media session discovery and transport commands via GSMTC.
/// This is used for both UI transport buttons and hardware media key forwarding.
/// </summary>
public sealed class MediaControlService : IDisposable
{
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private bool _disposed;

    public event EventHandler<MediaStateChangedEventArgs>? StateChanged;

    public async Task InitializeAsync()
    {
        if (_disposed || _sessionManager != null)
        {
            return;
        }

        _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;
        _sessionManager.SessionsChanged += OnSessionsChanged;

        SetCurrentSession(_sessionManager.GetCurrentSession());
        await PublishStateAsync();
    }

    public async Task<bool> PlayPauseAsync()
    {
        var session = GetControllableSession();
        if (session == null)
        {
            return false;
        }

        var playbackInfo = session.GetPlaybackInfo();
        var controls = playbackInfo?.Controls;

        bool success;
        if (playbackInfo?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
        {
            success = controls?.IsPauseEnabled == true && await session.TryPauseAsync();
        }
        else
        {
            success = controls?.IsPlayEnabled == true && await session.TryPlayAsync();
        }

        if (success)
        {
            await PublishStateAsync();
        }

        return success;
    }

    public async Task<bool> NextAsync()
    {
        var session = GetControllableSession();
        if (session == null)
        {
            return false;
        }

        var controls = session.GetPlaybackInfo()?.Controls;
        if (controls?.IsNextEnabled != true)
        {
            return false;
        }

        var success = await session.TrySkipNextAsync();
        if (success)
        {
            await PublishStateAsync();
        }

        return success;
    }

    public async Task<bool> PreviousAsync()
    {
        var session = GetControllableSession();
        if (session == null)
        {
            return false;
        }

        var controls = session.GetPlaybackInfo()?.Controls;
        if (controls?.IsPreviousEnabled != true)
        {
            return false;
        }

        var success = await session.TrySkipPreviousAsync();
        if (success)
        {
            await PublishStateAsync();
        }

        return success;
    }

    private GlobalSystemMediaTransportControlsSession? GetControllableSession()
    {
        if (_currentSession != null)
        {
            return _currentSession;
        }

        if (_sessionManager == null)
        {
            return null;
        }

        return _sessionManager.GetSessions()
            .FirstOrDefault(s =>
            {
                var controls = s.GetPlaybackInfo()?.Controls;
                return controls?.IsPlayEnabled == true ||
                       controls?.IsPauseEnabled == true ||
                       controls?.IsNextEnabled == true ||
                       controls?.IsPreviousEnabled == true;
            });
    }

    private void SetCurrentSession(GlobalSystemMediaTransportControlsSession? session)
    {
        if (ReferenceEquals(_currentSession, session))
        {
            return;
        }

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
        }
    }

    private async void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        SetCurrentSession(sender.GetCurrentSession());
        await PublishStateAsync();
    }

    private async void OnSessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args)
    {
        if (_currentSession == null)
        {
            SetCurrentSession(sender.GetCurrentSession());
        }

        await PublishStateAsync();
    }

    private async void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        await PublishStateAsync();
    }

    private async void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        await PublishStateAsync();
    }

    private async Task PublishStateAsync()
    {
        if (_disposed)
        {
            return;
        }

        var session = GetControllableSession();
        if (session == null)
        {
            StateChanged?.Invoke(this, new MediaStateChangedEventArgs
            {
                HasActiveSession = false,
                CanPlayPause = false,
                CanSkipNext = false,
                CanSkipPrevious = false,
                IsPlaying = false,
                Title = null,
                Artist = null
            });
            return;
        }

        string? title = null;
        string? artist = null;

        try
        {
            var mediaProps = await session.TryGetMediaPropertiesAsync();
            title = string.IsNullOrWhiteSpace(mediaProps?.Title) ? null : mediaProps.Title;
            artist = string.IsNullOrWhiteSpace(mediaProps?.Artist) ? null : mediaProps.Artist;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MediaControlService: failed to read media properties - {ex.Message}");
        }

        var playbackInfo = session.GetPlaybackInfo();
        var controls = playbackInfo?.Controls;

        StateChanged?.Invoke(this, new MediaStateChangedEventArgs
        {
            HasActiveSession = true,
            CanPlayPause = controls?.IsPlayEnabled == true || controls?.IsPauseEnabled == true,
            CanSkipNext = controls?.IsNextEnabled == true,
            CanSkipPrevious = controls?.IsPreviousEnabled == true,
            IsPlaying = playbackInfo?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
            Title = title,
            Artist = artist
        });
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
            _sessionManager.SessionsChanged -= OnSessionsChanged;
            _sessionManager = null;
        }
    }
}
