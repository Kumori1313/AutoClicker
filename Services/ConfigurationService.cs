using AutoClicker.Helpers;
using AutoClicker.Models;
using System.IO;
using System.Text;

namespace AutoClicker.Services;

public class ConfigurationService
{
    private const string DefaultFileName = "autoclicker_save.ini";

    public string GetDefaultFilePath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AutoClicker",
            DefaultFileName);
    }

    public void Save(ClickerConfiguration config, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var sb = new StringBuilder();

        // Interval section
        sb.AppendLine("[Interval]");
        sb.AppendLine($"Hours={config.Hours}");
        sb.AppendLine($"Minutes={config.Minutes}");
        sb.AppendLine($"Seconds={config.Seconds}");
        sb.AppendLine($"Milliseconds={config.Milliseconds}");
        sb.AppendLine();

        // Locations section
        sb.AppendLine("[Locations]");
        sb.AppendLine($"UseMousePosition={config.UseMousePosition}");
        sb.AppendLine($"Count={config.Locations.Count}");
        for (int i = 0; i < config.Locations.Count; i++)
        {
            sb.AppendLine($"Location{i}={config.Locations[i].X},{config.Locations[i].Y}");
        }
        sb.AppendLine();

        // Settings section
        sb.AppendLine("[Settings]");
        sb.AppendLine($"IsIndefinite={config.IsIndefinite}");
        sb.AppendLine($"IterationCount={config.IterationCount}");
        sb.AppendLine($"ToggleKey={config.ToggleKey}");
        sb.AppendLine($"MouseButton={config.MouseButton}");

        File.WriteAllText(filePath, sb.ToString());
    }

    public ClickerConfiguration Load(string filePath)
    {
        var config = new ClickerConfiguration();

        if (!File.Exists(filePath))
        {
            return config;
        }

        var lines = File.ReadAllLines(filePath);
        string currentSection = "";

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(';') || trimmed.StartsWith('#'))
            {
                continue;
            }

            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                currentSection = trimmed[1..^1];
                continue;
            }

            var parts = trimmed.Split('=', 2);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            switch (currentSection)
            {
                case "Interval":
                    ParseIntervalKey(config, key, value);
                    break;
                case "Locations":
                    ParseLocationsKey(config, key, value);
                    break;
                case "Settings":
                    ParseSettingsKey(config, key, value);
                    break;
            }
        }

        return config;
    }

    private void ParseIntervalKey(ClickerConfiguration config, string key, string value)
    {
        switch (key)
        {
            case "Hours":
                if (int.TryParse(value, out int hours)) config.Hours = hours;
                break;
            case "Minutes":
                if (int.TryParse(value, out int minutes)) config.Minutes = minutes;
                break;
            case "Seconds":
                if (int.TryParse(value, out int seconds)) config.Seconds = seconds;
                break;
            case "Milliseconds":
                if (int.TryParse(value, out int ms)) config.Milliseconds = ms;
                break;
        }
    }

    private void ParseLocationsKey(ClickerConfiguration config, string key, string value)
    {
        switch (key)
        {
            case "UseMousePosition":
                if (bool.TryParse(value, out bool useMousePos)) config.UseMousePosition = useMousePos;
                break;
            default:
                if (key.StartsWith("Location"))
                {
                    var coords = value.Split(',');
                    if (coords.Length == 2 &&
                        int.TryParse(coords[0], out int x) &&
                        int.TryParse(coords[1], out int y))
                    {
                        config.Locations.Add(new ClickLocation { X = x, Y = y });
                    }
                }
                break;
        }
    }

    private void ParseSettingsKey(ClickerConfiguration config, string key, string value)
    {
        switch (key)
        {
            case "IsIndefinite":
                if (bool.TryParse(value, out bool isIndefinite)) config.IsIndefinite = isIndefinite;
                break;
            case "IterationCount":
                if (int.TryParse(value, out int count)) config.IterationCount = count;
                break;
            case "ToggleKey":
                if (uint.TryParse(value, out uint toggleKey)) config.ToggleKey = toggleKey;
                break;
            case "MouseButton":
                if (Enum.TryParse<MouseButton>(value, out var button)) config.MouseButton = button;
                break;
        }
    }
}
