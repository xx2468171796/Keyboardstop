using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using KeyboardStop.Services;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace KeyboardStop.Views;

public partial class MainWindow : Window
{
    private readonly ConfigService _configService;
    private readonly InputMethodService _inputMethodService;
    private readonly HotkeyService _hotkeyService;
    private readonly LayoutService _layoutService;
    private readonly TrayService _trayService;
    private bool _isLocked = false;
    private bool _isCapturingHotkey = false;
    private int _pendingModifiers;
    private int _pendingKey;

    public MainWindow(
        ConfigService configService,
        InputMethodService inputMethodService,
        HotkeyService hotkeyService,
        LayoutService layoutService,
        TrayService trayService)
    {
        InitializeComponent();
        
        _configService = configService;
        _inputMethodService = inputMethodService;
        _hotkeyService = hotkeyService;
        _layoutService = layoutService;
        _trayService = trayService;

        // 加载配置
        LoadSettings();
        
        // 检查布局状态
        UpdateLayoutStatus();

        // 订阅热键事件
        _hotkeyService.HotkeyTriggered += OnHotkeyTriggered;
    }

    private void LoadSettings()
    {
        var config = _configService.Load();
        CorrectionCheckBox.IsChecked = config.EnableCorrectionPolling;
        StartupCheckBox.IsChecked = _configService.GetStartWithWindows();
        _pendingModifiers = config.HotkeyModifiers;
        _pendingKey = config.HotkeyKey;
        HotkeyTextBox.Text = config.GetHotkeyDisplayString();
    }

    private void UpdateLayoutStatus()
    {
        var isAvailable = _layoutService.IsUSLayoutAvailable();
        LayoutStatus.Text = isAvailable ? "✓ 已安装" : "⚠ 未安装";
        LayoutStatus.Foreground = isAvailable 
            ? System.Windows.Media.Brushes.Green 
            : System.Windows.Media.Brushes.Orange;
        AddLayoutButton.IsEnabled = !isAvailable;
        AddLayoutButton.Content = isAvailable ? "已安装" : "一键添加";
    }

    private void OnHotkeyTriggered()
    {
        Dispatcher.Invoke(() =>
        {
            ToggleLock();
        });
    }

    private void ToggleLock()
    {
        if (_isLocked)
        {
            _inputMethodService.Unlock();
            _isLocked = false;
            UpdateUI(false);
            _trayService.UpdateStatus(false);
            _trayService.ShowNotification("输入法锁定", "已解锁，恢复原输入源");
        }
        else
        {
            _inputMethodService.Lock();
            _isLocked = true;
            UpdateUI(true);
            _trayService.UpdateStatus(true);
            _trayService.ShowNotification("输入法锁定", "已锁定为英文输入");
        }
    }

    private void UpdateUI(bool locked)
    {
        if (locked)
        {
            StatusIcon.Text = "🔒";
            StatusText.Text = "当前状态：已锁定";
            StatusDesc.Text = "输入法已锁定为英文";
            ToggleButton.Content = "🔓 解锁输入法";
            ToggleButton.Background = System.Windows.Media.Brushes.Orange;
        }
        else
        {
            StatusIcon.Text = "🔓";
            StatusText.Text = "当前状态：已解锁";
            StatusDesc.Text = "输入法可自由切换";
            ToggleButton.Content = "🔒 锁定英文输入";
            ToggleButton.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(33, 150, 243));
        }
    }

    private void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleLock();
    }

    private void AddLayout_Click(object sender, RoutedEventArgs e)
    {
        var (success, message) = _layoutService.AddUSLayout();
        System.Windows.MessageBox.Show(message, success ? "成功" : "提示",
            MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
        UpdateLayoutStatus();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void HotkeyTextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        _isCapturingHotkey = true;
        HotkeyTextBox.Text = "请按下快捷键...";
        HotkeyTextBox.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(255, 243, 224));
    }

    private void HotkeyTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _isCapturingHotkey = false;
        var config = _configService.Load();
        config.HotkeyModifiers = _pendingModifiers;
        config.HotkeyKey = _pendingKey;
        HotkeyTextBox.Text = config.GetHotkeyDisplayString();
        HotkeyTextBox.Background = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(227, 242, 253));
    }

    private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isCapturingHotkey) return;
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        int modifiers = 0;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers |= 0x0001;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) modifiers |= 0x0002;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers |= 0x0004;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows)) modifiers |= 0x0008;

        if (modifiers == 0)
        {
            MessageBox.Show("请至少使用一个修饰键（Ctrl、Alt、Shift）", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _pendingModifiers = modifiers;
        _pendingKey = KeyInterop.VirtualKeyFromKey(key);

        var parts = new List<string>();
        if ((modifiers & 0x0002) != 0) parts.Add("Ctrl");
        if ((modifiers & 0x0001) != 0) parts.Add("Alt");
        if ((modifiers & 0x0004) != 0) parts.Add("Shift");
        if ((modifiers & 0x0008) != 0) parts.Add("Win");
        parts.Add(key.ToString());

        HotkeyTextBox.Text = string.Join(" + ", parts);
        _isCapturingHotkey = false;
        Keyboard.ClearFocus();
    }

    private void SaveHotkey_Click(object sender, RoutedEventArgs e)
    {
        _hotkeyService.UnregisterAll();
        
        var success = _hotkeyService.Register(_pendingModifiers, _pendingKey, () => { });
        
        if (!success)
        {
            MessageBox.Show("快捷键注册失败：该组合键可能被系统或其他程序占用，请更换。",
                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var config = _configService.Load();
        config.HotkeyModifiers = _pendingModifiers;
        config.HotkeyKey = _pendingKey;
        _configService.Save(config);

        MessageBox.Show($"快捷键已保存为：{config.GetHotkeyDisplayString()}", "成功",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized && MinimizeToTrayCheckBox.IsChecked == true)
        {
            Hide();
            _trayService.ShowNotification("KeyboardStop", "程序已最小化到系统托盘");
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // 保存设置
        var config = _configService.Load();
        config.EnableCorrectionPolling = CorrectionCheckBox.IsChecked ?? true;
        _configService.Save(config);
        _configService.SetStartWithWindows(StartupCheckBox.IsChecked ?? false);

        // 更新纠偏状态
        if (config.EnableCorrectionPolling)
            _inputMethodService.StartCorrectionPolling();
        else
            _inputMethodService.StopCorrectionPolling();

        // 取消订阅
        _hotkeyService.HotkeyTriggered -= OnHotkeyTriggered;
    }

    public void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }
}
