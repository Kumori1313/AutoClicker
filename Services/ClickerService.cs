using AutoClicker.Helpers;
using AutoClicker.Models;
using System.Diagnostics;
using System.Windows.Threading;

namespace AutoClicker.Services;

public class ClickerService : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private Thread? _clickThread;
    private CancellationTokenSource? _cts;
    private ClickerConfiguration? _config;
    private int _currentLocationIndex;
    private int _currentIteration;
    private bool _disposed;

    public bool IsRunning { get; private set; }

    public event Action? Started;
    public event Action? Stopped;
    public event Action<int>? IterationCompleted;

    public ClickerService(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void Start(ClickerConfiguration config)
    {
        if (IsRunning) return;

        _config = config.Clone();
        _currentLocationIndex = 0;
        _currentIteration = 0;
        _cts = new CancellationTokenSource();

        IsRunning = true;
        Started?.Invoke();

        _clickThread = new Thread(ClickLoop)
        {
            IsBackground = true,
            Priority = ThreadPriority.Highest
        };
        _clickThread.Start();
    }

    private void ClickLoop()
    {
        if (_config == null || _cts == null) return;

        var interval = _config.TotalIntervalMs;
        if (interval < 1) interval = 1;

        var stopwatch = Stopwatch.StartNew();
        var nextClickTime = 0L;

        while (!_cts.Token.IsCancellationRequested)
        {
            var currentTime = stopwatch.ElapsedMilliseconds;

            if (currentTime >= nextClickTime)
            {
                PerformClick();
                nextClickTime = currentTime + interval;

                if (!_config.IsIndefinite && _currentIteration >= _config.IterationCount)
                {
                    break;
                }
            }

            var remaining = nextClickTime - stopwatch.ElapsedMilliseconds;
            if (remaining > 15)
            {
                Thread.Sleep((int)(remaining - 10));
            }
            else if (remaining > 1)
            {
                Thread.SpinWait(100);
            }
        }

        _dispatcher.Invoke(() =>
        {
            IsRunning = false;
            Stopped?.Invoke();
        });
    }

    public void Stop()
    {
        if (!IsRunning) return;

        _cts?.Cancel();
        _clickThread?.Join(1000);
        _cts?.Dispose();
        _cts = null;
        _clickThread = null;

        if (IsRunning)
        {
            IsRunning = false;
            Stopped?.Invoke();
        }
    }

    public void Toggle(ClickerConfiguration config)
    {
        if (IsRunning)
        {
            Stop();
        }
        else
        {
            Start(config);
        }
    }

    private void PerformClick()
    {
        if (_config == null) return;

        if (_config.UseMousePosition)
        {
            NativeMethods.SimulateClickAtCurrentPosition(_config.MouseButton);
        }
        else if (_config.Locations.Count > 0)
        {
            var location = _config.Locations[_currentLocationIndex];
            NativeMethods.SimulateClick(location.X, location.Y, _config.MouseButton);
            _currentLocationIndex = (_currentLocationIndex + 1) % _config.Locations.Count;
        }

        if (!_config.IsIndefinite)
        {
            _currentIteration++;
            var iteration = _currentIteration;
            _dispatcher.BeginInvoke(() => IterationCompleted?.Invoke(iteration));
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~ClickerService()
    {
        Dispose();
    }
}
