using System.Collections.Concurrent;
using System.Diagnostics;
using Windows.Media.Audio;

namespace BtAudioSink.Bluetooth;

/// <summary>
/// Event arguments for device connection state changes.
/// </summary>
public sealed class DeviceConnectionChangedEventArgs : EventArgs
{
    public required string DeviceId { get; init; }
    public required bool IsConnected { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Manages AudioPlaybackConnection instances for A2DP Sink audio streaming.
/// Handles connect, disconnect, state monitoring, and auto-reconnect.
/// </summary>
public sealed class AudioPlaybackService : IDisposable
{
    private readonly ConcurrentDictionary<string, AudioPlaybackConnection> _connections = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _reconnectCts = new();
    private bool _disposed;
    private bool _autoReconnect;

    private const int MaxReconnectAttempts = 4;
    private static readonly int[] ReconnectDelaysMs = [2000, 5000, 10000, 30000];

    /// <summary>
    /// Fired when a device's connection state changes.
    /// </summary>
    public event EventHandler<DeviceConnectionChangedEventArgs>? ConnectionChanged;

    /// <summary>
    /// Gets or sets whether auto-reconnect is enabled.
    /// </summary>
    public bool AutoReconnect
    {
        get => _autoReconnect;
        set => _autoReconnect = value;
    }

    /// <summary>
    /// Gets the set of currently connected device IDs.
    /// </summary>
    public IReadOnlyCollection<string> ConnectedDeviceIds => _connections.Keys.ToList().AsReadOnly();

    /// <summary>
    /// Returns true if the given device is currently connected.
    /// </summary>
    public bool IsConnected(string deviceId) => _connections.ContainsKey(deviceId);

    /// <summary>
    /// Connects to a Bluetooth device for A2DP audio playback.
    /// Creates an AudioPlaybackConnection, starts and opens it.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> ConnectAsync(string deviceId)
    {
        if (_disposed)
        {
            return (false, "Service is disposed");
        }

        // If already connected, disconnect first
        if (_connections.ContainsKey(deviceId))
        {
            await DisconnectAsync(deviceId);
        }

        // Cancel any pending reconnect for this device
        CancelReconnect(deviceId);

        try
        {
            var connection = AudioPlaybackConnection.TryCreateFromId(deviceId);
            if (connection == null)
            {
                return (false, "Failed to create audio playback connection. Device may not support A2DP Sink.");
            }

            // Monitor connection state changes
            connection.StateChanged += (sender, _) =>
            {
                if (sender.State == AudioPlaybackConnectionState.Closed)
                {
                    OnConnectionClosed(sender.DeviceId);
                }
            };

            // Start the audio pipeline
            await connection.StartAsync();

            // Open the connection for audio streaming
            var result = await connection.OpenAsync();

            switch (result.Status)
            {
                case AudioPlaybackConnectionOpenResultStatus.Success:
                    _connections[deviceId] = connection;
                    Debug.WriteLine($"AudioPlaybackService: Connected to {deviceId}");
                    ConnectionChanged?.Invoke(this, new DeviceConnectionChangedEventArgs
                    {
                        DeviceId = deviceId,
                        IsConnected = true
                    });
                    return (true, null);

                case AudioPlaybackConnectionOpenResultStatus.RequestTimedOut:
                    connection.Dispose();
                    return (false, "Connection request timed out");

                case AudioPlaybackConnectionOpenResultStatus.DeniedBySystem:
                    connection.Dispose();
                    return (false, "Connection denied by the system");

                case AudioPlaybackConnectionOpenResultStatus.UnknownFailure:
                default:
                    var errorCode = result.ExtendedError;
                    connection.Dispose();
                    return (false, $"Unknown failure (0x{errorCode:X8})");
            }
        }
        catch (Exception ex)
        {
            return (false, $"{ex.Message} (0x{ex.HResult:X8})");
        }
    }

    /// <summary>
    /// Disconnects a specific device.
    /// </summary>
    public Task DisconnectAsync(string deviceId)
    {
        CancelReconnect(deviceId);

        if (_connections.TryRemove(deviceId, out var connection))
        {
            try
            {
                connection.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AudioPlaybackService: Error disconnecting {deviceId} - {ex.Message}");
            }

            Debug.WriteLine($"AudioPlaybackService: Disconnected from {deviceId}");
            ConnectionChanged?.Invoke(this, new DeviceConnectionChangedEventArgs
            {
                DeviceId = deviceId,
                IsConnected = false
            });
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Disconnects all connected devices.
    /// </summary>
    public void DisconnectAll()
    {
        foreach (var deviceId in _connections.Keys.ToList())
        {
            CancelReconnect(deviceId);

            if (_connections.TryRemove(deviceId, out var connection))
            {
                try
                {
                    connection.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"AudioPlaybackService: Error disconnecting {deviceId} - {ex.Message}");
                }
            }

            ConnectionChanged?.Invoke(this, new DeviceConnectionChangedEventArgs
            {
                DeviceId = deviceId,
                IsConnected = false
            });
        }
    }

    /// <summary>
    /// Called when a connection is closed (e.g., device went out of range, manually disconnected from phone).
    /// </summary>
    private void OnConnectionClosed(string deviceId)
    {
        bool wasConnected = _connections.TryRemove(deviceId, out var connection);
        if (wasConnected)
        {
            try
            {
                connection?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AudioPlaybackService: Error cleaning up closed connection for {deviceId} - {ex.Message}");
            }

            Debug.WriteLine($"AudioPlaybackService: Connection closed for {deviceId}");
            ConnectionChanged?.Invoke(this, new DeviceConnectionChangedEventArgs
            {
                DeviceId = deviceId,
                IsConnected = false,
                ErrorMessage = "Connection closed by remote device"
            });

            // Attempt auto-reconnect if enabled
            if (_autoReconnect && !_disposed)
            {
                ScheduleReconnect(deviceId);
            }
        }
    }

    /// <summary>
    /// Schedules an auto-reconnect attempt with exponential backoff.
    /// </summary>
    private void ScheduleReconnect(string deviceId)
    {
        CancelReconnect(deviceId);

        var cts = new CancellationTokenSource();
        _reconnectCts[deviceId] = cts;

        _ = Task.Run(async () =>
        {
            for (int attempt = 0; attempt < MaxReconnectAttempts; attempt++)
            {
                if (cts.Token.IsCancellationRequested || _disposed)
                {
                    return;
                }

                int delay = attempt < ReconnectDelaysMs.Length
                    ? ReconnectDelaysMs[attempt]
                    : ReconnectDelaysMs[^1];

                Debug.WriteLine($"AudioPlaybackService: Reconnect attempt {attempt + 1}/{MaxReconnectAttempts} for {deviceId} in {delay}ms");

                try
                {
                    await Task.Delay(delay, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (cts.Token.IsCancellationRequested || _disposed)
                {
                    return;
                }

                var (success, error) = await ConnectAsync(deviceId);
                if (success)
                {
                    Debug.WriteLine($"AudioPlaybackService: Auto-reconnected to {deviceId}");
                    return;
                }

                Debug.WriteLine($"AudioPlaybackService: Reconnect attempt {attempt + 1} failed for {deviceId}: {error}");
            }

            Debug.WriteLine($"AudioPlaybackService: Gave up reconnecting to {deviceId} after {MaxReconnectAttempts} attempts");
        }, cts.Token);
    }

    /// <summary>
    /// Cancels any pending reconnect attempt for a device.
    /// </summary>
    private void CancelReconnect(string deviceId)
    {
        if (_reconnectCts.TryRemove(deviceId, out var cts))
        {
            try
            {
                cts.Cancel();
                cts.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Cancel all reconnect attempts
        foreach (var deviceId in _reconnectCts.Keys.ToList())
        {
            CancelReconnect(deviceId);
        }

        // Disconnect all devices
        DisconnectAll();
    }
}
