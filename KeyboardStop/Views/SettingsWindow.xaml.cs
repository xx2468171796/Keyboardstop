using System.Windows;
using System.Windows.Input;
using KeyboardStop.Models;
using KeyboardStop.Services;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace KeyboardStop.Views;

public partial class SettingsWindow : Window
{
    private readonly ConfigService _configService;
    private readonly HotkeyService _hotkeyService;
    private readonly LayoutService _layoutService;
    private readonly InputMethodService _inputMethodService;
    
    private int _pendingModifiers;
    private int _pendingKey;
    private bool _isCapturingHotkey;

    public SettingsWindow(
        ConfigService configService,
        HotkeyService hotkeyService,
        LayoutService layoutService,
        InputMethodService inputMethodService)
    {
        InitializeComponent();
        
        _configService = configService;
        _hotkeyService = hotkeyService;
        _layoutService = layoutService;
        _inputMethodService = inputMethodService;

        LoadSettings();
        UpdateLayoutStatus();
    }

    private void LoadSettings()
    {
        var config = _configService.Load();
        
        _pendingModifiers = config.HotkeyModifiers;
        _pendingKey = config.HotkeyKey;
        HotkeyTextBox.Text = config.GetHotkeyDisplayString();
        
        CorrectionPollingCheckBox.IsChecked = config.EnableCorrectionPolling;
        StartWithWindowsCheckBox.IsChecked = _configService.GetStartWithWindows();
        StartLockedCheckBox.IsChecked = config.StartLocked;
    }

    private void UpdateLayoutStatus()
    {
        var isAvailable = _layoutService.IsUSLayoutAvailable();
        LayoutStatusText.Text = isAvailable 
            ? "✓ English (US) 键盘布局已安装" 
            : "⚠ 未检测到 English (US) 键盘布局";
        AddLayoutButton.IsEnabled = !isAvailable;
        AddLayoutButton.Content = isAvailable ? "已安装" : "一键添加 US 布局";
    }

    private void HotkeyTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _isCapturingHotkey = true;
        HotkeyTextBox.Text = "请按下快捷键组合...";
    }

    private void HotkeyTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _isCapturingHotkey = false;
        var config = _configService.Load();
        config.HotkeyModifiers = _pendingModifiers;
        config.HotkeyKey = _pendingKey;
        HotkeyTextBox.Text = config.GetHotkeyDisplayString();
    }

    private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isCapturingHotkey) return;

        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        
        // 忽略单独的修饰键
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        // 构建修饰键
        int modifiers = 0;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers |= 0x0001;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) modifiers |= 0x0002;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers |= 0x0004;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) modifiers |= 0x0008;

        // 至少需要一个修饰键
        if (modifiers == 0)
        {
            MessageBox.Show("请至少使用一个修饰键（Ctrl、Alt、Shift 或 Win）", "提示", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _pendingModifiers = modifiers;
        _pendingKey = KeyInterop.VirtualKeyFromKey(key);

        // 显示快捷键
        var parts = new List<string>();
        if ((modifiers & 0x0002) != 0) parts.Add("Ctrl");
        if ((modifiers & 0x0001) != 0) parts.Add("Alt");
        if ((modifiers & 0x0004) != 0) parts.Add("Shift");
        if ((modifiers & 0x0008) != 0) parts.Add("Win");
        parts.Add(key.ToString());

        HotkeyTextBox.Text = string.Join(" + ", parts);
        _isCapturingHotkey = false;
    }

    private void TestHotkey_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show($"当前快捷键: {HotkeyTextBox.Text}\n\n保存后生效。", "快捷键测试", 
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AddLayout_Click(object sender, RoutedEventArgs e)
    {
        var (success, message) = _layoutService.AddUSLayout();
        MessageBox.Show(message, success ? "成功" : "提示", 
            MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        UpdateLayoutStatus();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // 注销旧热键
        _hotkeyService.UnregisterAll();

        // 注册新热键
        var success = _hotkeyService.Register(_pendingModifiers, _pendingKey, () => { });
        
        if (!success)
        {
            MessageBox.Show("快捷键注册失败：该组合键可能被系统或其他程序占用，请更换。", 
                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // 保存配置
        var config = _configService.Load();
        config.HotkeyModifiers = _pendingModifiers;
        config.HotkeyKey = _pendingKey;
        config.EnableCorrectionPolling = CorrectionPollingCheckBox.IsChecked ?? true;
        config.StartLocked = StartLockedCheckBox.IsChecked ?? false;
        _configService.Save(config);

        // 设置开机自启
        _configService.SetStartWithWindows(StartWithWindowsCheckBox.IsChecked ?? false);

        // 更新纠偏轮询状态
        if (config.EnableCorrectionPolling)
        {
            _inputMethodService.StartCorrectionPolling(config.PollingIntervalMs);
        }
        else
        {
            _inputMethodService.StopCorrectionPolling();
        }

        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
