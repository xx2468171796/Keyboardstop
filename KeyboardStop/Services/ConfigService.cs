using System.IO;
using System.Text.Json;
using KeyboardStop.Models;
using Microsoft.Win32;

namespace KeyboardStop.Services;

public class ConfigService
{
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private AppConfig? _cachedConfig;

    public ConfigService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDir = Path.Combine(appDataPath, "KeyboardStop");
        Directory.CreateDirectory(configDir);
        _configPath = Path.Combine(configDir, "config.json");
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }

    public AppConfig Load()
    {
        if (_cachedConfig != null) return _cachedConfig;

        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                _cachedConfig = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions) ?? new AppConfig();
            }
            else
            {
                _cachedConfig = new AppConfig();
                Save(_cachedConfig);
            }
        }
        catch
        {
            _cachedConfig = new AppConfig();
        }

        return _cachedConfig;
    }

    public void Save(AppConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(_configPath, json);
            _cachedConfig = config;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存配置失败: {ex.Message}");
        }
    }

    public bool GetStartWithWindows()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue("KeyboardStop") != null;
        }
        catch
        {
            return false;
        }
    }

    public void SetStartWithWindows(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;

            if (enable)
            {
                var exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                key.SetValue("KeyboardStop", $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue("KeyboardStop", false);
            }

            var config = Load();
            config.StartWithWindows = enable;
            Save(config);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"设置开机自启失败: {ex.Message}");
        }
    }
}
