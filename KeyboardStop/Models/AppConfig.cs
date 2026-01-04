using System.Windows.Forms;

namespace KeyboardStop.Models;

public class AppConfig
{
    public int HotkeyModifiers { get; set; } = 0x0003; // Ctrl + Alt
    public int HotkeyKey { get; set; } = 0x4B; // K
    public bool EnableCorrectionPolling { get; set; } = true;
    public int PollingIntervalMs { get; set; } = 300;
    public bool StartWithWindows { get; set; } = false;
    public bool StartLocked { get; set; } = false;
    public bool EnableLogging { get; set; } = false;
    
    public string GetHotkeyDisplayString()
    {
        var parts = new List<string>();
        if ((HotkeyModifiers & 0x0002) != 0) parts.Add("Ctrl");
        if ((HotkeyModifiers & 0x0001) != 0) parts.Add("Alt");
        if ((HotkeyModifiers & 0x0004) != 0) parts.Add("Shift");
        if ((HotkeyModifiers & 0x0008) != 0) parts.Add("Win");
        
        var keyName = ((Keys)HotkeyKey).ToString();
        parts.Add(keyName);
        
        return string.Join(" + ", parts);
    }
}
