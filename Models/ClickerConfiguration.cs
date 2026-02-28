using AutoClicker.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AutoClicker.Models;

public class ClickerConfiguration : INotifyPropertyChanged
{
    private int _hours;
    private int _minutes;
    private int _seconds = 1;
    private int _milliseconds;
    private bool _useMousePosition = true;
    private List<ClickLocation> _locations = new();
    private bool _isIndefinite = true;
    private int _iterationCount = 10;
    private uint _toggleKey = NativeMethods.VK_INSERT;
    private MouseButton _mouseButton = MouseButton.Left;

    public int Hours
    {
        get => _hours;
        set { _hours = Math.Max(0, value); OnPropertyChanged(); OnPropertyChanged(nameof(TotalIntervalMs)); }
    }

    public int Minutes
    {
        get => _minutes;
        set { _minutes = Math.Clamp(value, 0, 59); OnPropertyChanged(); OnPropertyChanged(nameof(TotalIntervalMs)); }
    }

    public int Seconds
    {
        get => _seconds;
        set { _seconds = Math.Clamp(value, 0, 59); OnPropertyChanged(); OnPropertyChanged(nameof(TotalIntervalMs)); }
    }

    public int Milliseconds
    {
        get => _milliseconds;
        set { _milliseconds = Math.Clamp(value, 0, 999); OnPropertyChanged(); OnPropertyChanged(nameof(TotalIntervalMs)); }
    }

    public long TotalIntervalMs =>
        (Hours * 3600000L) + (Minutes * 60000L) + (Seconds * 1000L) + Milliseconds;

    public bool UseMousePosition
    {
        get => _useMousePosition;
        set { _useMousePosition = value; OnPropertyChanged(); }
    }

    public List<ClickLocation> Locations
    {
        get => _locations;
        set { _locations = value; OnPropertyChanged(); }
    }

    public bool IsIndefinite
    {
        get => _isIndefinite;
        set { _isIndefinite = value; OnPropertyChanged(); }
    }

    public int IterationCount
    {
        get => _iterationCount;
        set { _iterationCount = Math.Max(1, value); OnPropertyChanged(); }
    }

    public uint ToggleKey
    {
        get => _toggleKey;
        set { _toggleKey = value; OnPropertyChanged(); OnPropertyChanged(nameof(ToggleKeyName)); }
    }

    public string ToggleKeyName => NativeMethods.GetKeyName(_toggleKey);

    public MouseButton MouseButton
    {
        get => _mouseButton;
        set { _mouseButton = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ClickerConfiguration Clone()
    {
        return new ClickerConfiguration
        {
            Hours = Hours,
            Minutes = Minutes,
            Seconds = Seconds,
            Milliseconds = Milliseconds,
            UseMousePosition = UseMousePosition,
            Locations = new List<ClickLocation>(Locations.Select(l => new ClickLocation { X = l.X, Y = l.Y })),
            IsIndefinite = IsIndefinite,
            IterationCount = IterationCount,
            ToggleKey = ToggleKey,
            MouseButton = MouseButton
        };
    }
}

public class ClickLocation
{
    public int X { get; set; }
    public int Y { get; set; }

    public override string ToString() => $"({X}, {Y})";
}
