namespace BtAudioSink.Bluetooth;

/// <summary>
/// Represents a discovered Bluetooth device capable of A2DP audio streaming.
/// </summary>
public sealed class BluetoothDeviceInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public bool IsPaired { get; init; }
    public bool IsConnected { get; set; }

    public override string ToString() => $"{Name} ({(IsConnected ? "Connected" : "Available")})";
}
