using System.Windows;
using KeyboardStop.Services;
using KeyboardStop.Views;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace KeyboardStop;

public partial class App : Application
{
    private TrayService? _trayService;
    private HotkeyService? _hotkeyService;
    private InputMethodService? _inputMethodService;
    private ConfigService? _configService;
    private LayoutService? _layoutService;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 初始化服务
        _configService = new ConfigService();
        _layoutService = new LayoutService();
        _inputMethodService = new InputMethodService();
        _hotkeyService = new HotkeyService();
        _trayService = new TrayService(_configService, _inputMethodService, _hotkeyService, _layoutService);

        // 注册全局热键
        var config = _configService.Load();
        var registered = _hotkeyService.Register(config.HotkeyModifiers, config.HotkeyKey, () => { });
        
        if (!registered)
        {
            MessageBox.Show("快捷键注册失败：该组合键可能被系统或其他程序占用。\n请在设置中更换快捷键。",
                "KeyboardStop", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // 创建并显示主窗口
        _mainWindow = new MainWindow(_configService, _inputMethodService, _hotkeyService, _layoutService, _trayService);
        _trayService.SetMainWindow(_mainWindow);
        _mainWindow.Show();

        // 检查 US 布局
        if (!_layoutService.IsUSLayoutAvailable())
        {
            _trayService.ShowNotification("提示", "未检测到 US 键盘布局，可一键添加");
        }

        // 启动纠偏轮询（如果配置启用）
        if (config.EnableCorrectionPolling)
        {
            _inputMethodService.StartCorrectionPolling();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.UnregisterAll();
        _hotkeyService?.Dispose();
        _inputMethodService?.StopCorrectionPolling();
        _trayService?.Dispose();
        base.OnExit(e);
    }
}
