using AutoClicker.Helpers;
using AutoClicker.Models;
using AutoClicker.Services;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AutoClicker;

public partial class MainWindow : Window
{
    private const string RegistryKeyPath = @"Software\AutoClicker";
    private const string RegistryValueName = "ConfigFilePath";

    private readonly ClickerConfiguration _config = new();
    private readonly ConfigurationService _configService = new();
    private readonly HotkeyService _hotkeyService = new();
    private readonly ClickerService _clickerService;

    private string _configFilePath;
    private bool _isChoosingLocation;
    private System.Timers.Timer? _locationPickTimer;
    private bool _wasMouseDown;

    public MainWindow()
    {
        InitializeComponent();

        _clickerService = new ClickerService(Dispatcher);
        _configFilePath = GetConfigFilePathFromRegistry();

        LoadDefaultConfiguration();
        InitializeUI();
        SetupServices();
    }

    private string GetConfigFilePathFromRegistry()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
            if (key?.GetValue(RegistryValueName) is string path && !string.IsNullOrEmpty(path))
            {
                return path;
            }
        }
        catch
        {
            // Ignore registry errors
        }

        return _configService.GetDefaultFilePath();
    }

    private void SetConfigFilePathInRegistry(string path)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
            key?.SetValue(RegistryValueName, path);
        }
        catch
        {
            // Ignore registry errors
        }
    }

    private void LoadDefaultConfiguration()
    {
        try
        {
            if (System.IO.File.Exists(_configFilePath))
            {
                var loadedConfig = _configService.Load(_configFilePath);
                _config.Hours = loadedConfig.Hours;
                _config.Minutes = loadedConfig.Minutes;
                _config.Seconds = loadedConfig.Seconds;
                _config.Milliseconds = loadedConfig.Milliseconds;
                _config.UseMousePosition = loadedConfig.UseMousePosition;
                _config.Locations = loadedConfig.Locations;
                _config.IsIndefinite = loadedConfig.IsIndefinite;
                _config.IterationCount = loadedConfig.IterationCount;
                _config.ToggleKey = loadedConfig.ToggleKey;
                _config.MouseButton = loadedConfig.MouseButton;
            }
        }
        catch
        {
            // Silently ignore load errors on startup, use defaults
        }
    }

    private void SaveDefaultConfiguration()
    {
        try
        {
            _configService.Save(_config, _configFilePath);
        }
        catch
        {
            // Silently ignore save errors
        }
    }

    private void InitializeUI()
    {
        HoursBox.Text = _config.Hours.ToString();
        MinutesBox.Text = _config.Minutes.ToString();
        SecondsBox.Text = _config.Seconds.ToString();
        MillisecondsBox.Text = _config.Milliseconds.ToString();
        IterationCountBox.Text = _config.IterationCount.ToString();
        ToggleKeyText.Text = _config.ToggleKeyName;
        ConfigPathBox.Text = _configFilePath;

        MouseButtonCombo.SelectedIndex = _config.MouseButton switch
        {
            MouseButton.Right => 1,
            MouseButton.Middle => 2,
            _ => 0
        };

        MousePositionRadio.IsChecked = _config.UseMousePosition;
        PredeterminedRadio.IsChecked = !_config.UseMousePosition;
        LocationsPanel.Visibility = _config.UseMousePosition ? Visibility.Collapsed : Visibility.Visible;
        RefreshLocationsList();

        IndefiniteRadio.IsChecked = _config.IsIndefinite;
        SetCountRadio.IsChecked = !_config.IsIndefinite;
        IterationCountPanel.Visibility = _config.IsIndefinite ? Visibility.Collapsed : Visibility.Visible;

        UpdateStartStopButton();
    }

    private void SetupServices()
    {
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        _hotkeyService.KeyCaptured += OnKeyCaptured;
        _hotkeyService.Start(_config.ToggleKey);

        _clickerService.Started += OnClickerStarted;
        _clickerService.Stopped += OnClickerStopped;
        _clickerService.IterationCompleted += OnIterationCompleted;
    }

    private void OnHotkeyPressed()
    {
        Dispatcher.Invoke(() => _clickerService.Toggle(_config));
    }

    private void OnKeyCaptured(uint vkCode)
    {
        Dispatcher.Invoke(() =>
        {
            _config.ToggleKey = vkCode;
            ToggleKeyText.Text = _config.ToggleKeyName;
            RebindInstructions.Visibility = Visibility.Collapsed;
            RebindButton.IsEnabled = true;
            _hotkeyService.UpdateKey(vkCode);
            UpdateStartStopButton();
            SaveDefaultConfiguration();
        });
    }

    private void OnClickerStarted()
    {
        Dispatcher.Invoke(() =>
        {
            StartStopButton.Content = $"Stop ({_config.ToggleKeyName})";
            StartStopButton.Background = new SolidColorBrush(Colors.IndianRed);
            StatusText.Text = "Running...";
        });
    }

    private void OnClickerStopped()
    {
        Dispatcher.Invoke(() =>
        {
            UpdateStartStopButton();
            StartStopButton.ClearValue(BackgroundProperty);
            StatusText.Text = "Stopped";
        });
    }

    private void OnIterationCompleted(int count)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = $"Clicks: {count} / {_config.IterationCount}";
        });
    }

    private void UpdateStartStopButton()
    {
        StartStopButton.Content = $"Start ({_config.ToggleKeyName})";
    }

    #region Event Handlers

    private void IntervalChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox) return;

        if (int.TryParse(textBox.Text, out int value))
        {
            if (textBox == HoursBox) _config.Hours = value;
            else if (textBox == MinutesBox) _config.Minutes = value;
            else if (textBox == SecondsBox) _config.Seconds = value;
            else if (textBox == MillisecondsBox) _config.Milliseconds = value;
        }
    }

    private void MouseButtonChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MouseButtonCombo == null) return;

        _config.MouseButton = MouseButtonCombo.SelectedIndex switch
        {
            1 => MouseButton.Right,
            2 => MouseButton.Middle,
            _ => MouseButton.Left
        };
    }

    private void LocationModeChanged(object sender, RoutedEventArgs e)
    {
        if (LocationsPanel == null) return;

        _config.UseMousePosition = MousePositionRadio.IsChecked == true;
        LocationsPanel.Visibility = _config.UseMousePosition ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ChooseLocationToggle_Click(object sender, RoutedEventArgs e)
    {
        if (ChooseLocationToggle.IsChecked == true)
        {
            _isChoosingLocation = true;
            LocationInstructions.Visibility = Visibility.Visible;

            _locationPickTimer = new System.Timers.Timer(50);
            _locationPickTimer.Elapsed += CheckForLocationClick;
            _locationPickTimer.Start();
        }
        else
        {
            StopLocationPicking();
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private void CheckForLocationClick(object? sender, System.Timers.ElapsedEventArgs e)
    {
        bool isMouseDown = (GetAsyncKeyState(0x01) & 0x8000) != 0;

        if (_wasMouseDown && !isMouseDown && _isChoosingLocation)
        {
            NativeMethods.GetCursorPos(out var point);

            Dispatcher.Invoke(() =>
            {
                var location = new ClickLocation { X = point.X, Y = point.Y };
                _config.Locations.Add(location);
                RefreshLocationsList();
            });
        }

        _wasMouseDown = isMouseDown;
    }

    private void RefreshLocationsList()
    {
        LocationsList.ItemsSource = null;
        LocationsList.ItemsSource = _config.Locations;
    }

    private void StopLocationPicking()
    {
        _isChoosingLocation = false;
        _locationPickTimer?.Stop();
        _locationPickTimer?.Dispose();
        _locationPickTimer = null;

        Dispatcher.Invoke(() =>
        {
            ChooseLocationToggle.IsChecked = false;
            LocationInstructions.Visibility = Visibility.Collapsed;
        });
    }

    private void RemoveLocation_Click(object sender, RoutedEventArgs e)
    {
        if (LocationsList.SelectedItem is ClickLocation location)
        {
            _config.Locations.Remove(location);
            RefreshLocationsList();
        }
    }

    private void IterationModeChanged(object sender, RoutedEventArgs e)
    {
        if (IterationCountPanel == null) return;

        _config.IsIndefinite = IndefiniteRadio.IsChecked == true;
        IterationCountPanel.Visibility = _config.IsIndefinite ? Visibility.Collapsed : Visibility.Visible;
    }

    private void IterationCountChanged(object sender, TextChangedEventArgs e)
    {
        if (int.TryParse(IterationCountBox.Text, out int value))
        {
            _config.IterationCount = value;
        }
    }

    private void RebindButton_Click(object sender, RoutedEventArgs e)
    {
        RebindButton.IsEnabled = false;
        RebindInstructions.Visibility = Visibility.Visible;
        _hotkeyService.StartCapturing();
    }

    private void BrowseConfigPath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "INI Files (*.ini)|*.ini",
            DefaultExt = ".ini",
            FileName = System.IO.Path.GetFileName(_configFilePath),
            InitialDirectory = System.IO.Path.GetDirectoryName(_configFilePath),
            Title = "Choose Settings File Location",
            OverwritePrompt = false
        };

        if (dialog.ShowDialog() == true)
        {
            _configFilePath = dialog.FileName;
            ConfigPathBox.Text = _configFilePath;
            SetConfigFilePathInRegistry(_configFilePath);

            // Load config from new path if it exists, otherwise save current config there
            if (System.IO.File.Exists(_configFilePath))
            {
                try
                {
                    var loadedConfig = _configService.Load(_configFilePath);
                    ApplyConfiguration(loadedConfig);
                    StatusText.Text = "Settings loaded from new path!";
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"Load failed: {ex.Message}";
                }
            }
            else
            {
                SaveDefaultConfiguration();
                StatusText.Text = "Settings path updated!";
            }
        }
    }

    private void SaveConfig_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "INI Files (*.ini)|*.ini",
            DefaultExt = ".ini",
            FileName = "autoclicker_save"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _configService.Save(_config, dialog.FileName);
                StatusText.Text = "Configuration saved!";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Save failed: {ex.Message}";
            }
        }
    }

    private void LoadConfig_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "INI Files (*.ini)|*.ini",
            DefaultExt = ".ini"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var loadedConfig = _configService.Load(dialog.FileName);
                ApplyConfiguration(loadedConfig);
                StatusText.Text = "Configuration loaded!";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Load failed: {ex.Message}";
            }
        }
    }

    private void ApplyConfiguration(ClickerConfiguration config)
    {
        _config.Hours = config.Hours;
        _config.Minutes = config.Minutes;
        _config.Seconds = config.Seconds;
        _config.Milliseconds = config.Milliseconds;
        _config.UseMousePosition = config.UseMousePosition;
        _config.Locations = config.Locations;
        _config.IsIndefinite = config.IsIndefinite;
        _config.IterationCount = config.IterationCount;
        _config.ToggleKey = config.ToggleKey;
        _config.MouseButton = config.MouseButton;

        HoursBox.Text = config.Hours.ToString();
        MinutesBox.Text = config.Minutes.ToString();
        SecondsBox.Text = config.Seconds.ToString();
        MillisecondsBox.Text = config.Milliseconds.ToString();

        MouseButtonCombo.SelectedIndex = config.MouseButton switch
        {
            MouseButton.Right => 1,
            MouseButton.Middle => 2,
            _ => 0
        };

        MousePositionRadio.IsChecked = config.UseMousePosition;
        PredeterminedRadio.IsChecked = !config.UseMousePosition;
        LocationsPanel.Visibility = config.UseMousePosition ? Visibility.Collapsed : Visibility.Visible;
        RefreshLocationsList();

        IndefiniteRadio.IsChecked = config.IsIndefinite;
        SetCountRadio.IsChecked = !config.IsIndefinite;
        IterationCountPanel.Visibility = config.IsIndefinite ? Visibility.Collapsed : Visibility.Visible;
        IterationCountBox.Text = config.IterationCount.ToString();

        ToggleKeyText.Text = config.ToggleKeyName;
        _hotkeyService.UpdateKey(config.ToggleKey);
        UpdateStartStopButton();
    }

    private void StartStop_Click(object sender, RoutedEventArgs e)
    {
        _clickerService.Toggle(_config);
    }

    #endregion

    protected override void OnClosed(EventArgs e)
    {
        _hotkeyService.Dispose();
        _clickerService.Dispose();
        base.OnClosed(e);
    }
}
