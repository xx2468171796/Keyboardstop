using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using KeyboardStop.Resources;

namespace KeyboardStop.Services;

public class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ConfigService _configService;
    private readonly InputMethodService _inputMethodService;
    private readonly HotkeyService _hotkeyService;
    private readonly LayoutService _layoutService;
    
    private ToolStripMenuItem? _lockMenuItem;
    private ToolStripMenuItem? _addLayoutMenuItem;
    private bool _disposed;
    private Icon? _unlockedIcon;
    private Icon? _lockedIcon;
    private Views.MainWindow? _mainWindow;

    public TrayService(
        ConfigService configService,
        InputMethodService inputMethodService,
        HotkeyService hotkeyService,
        LayoutService layoutService)
    {
        _configService = configService;
        _inputMethodService = inputMethodService;
        _hotkeyService = hotkeyService;
        _layoutService = layoutService;

        // 创建图标
        _unlockedIcon = Icons.CreateDefaultIcon();
        _lockedIcon = Icons.CreateLockedIcon();

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Text = "KeyboardStop - 游戏输入法锁定",
            Icon = _unlockedIcon
        };

        CreateContextMenu();

        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();
    }

    public void SetMainWindow(Views.MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }

    private void CreateContextMenu()
    {
        var contextMenu = new ContextMenuStrip();

        // 锁定英文模式
        _lockMenuItem = new ToolStripMenuItem("锁定英文模式")
        {
            CheckOnClick = true
        };
        _lockMenuItem.Click += OnLockMenuClick;
        contextMenu.Items.Add(_lockMenuItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        // 一键添加 US 布局
        _addLayoutMenuItem = new ToolStripMenuItem("一键添加美式键盘（US）");
        _addLayoutMenuItem.Click += OnAddLayoutClick;
        UpdateAddLayoutMenuVisibility();
        contextMenu.Items.Add(_addLayoutMenuItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        // 打开主窗口
        var showWindowItem = new ToolStripMenuItem("打开主窗口");
        showWindowItem.Click += (s, e) => ShowMainWindow();
        contextMenu.Items.Add(showWindowItem);

        // 退出
        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (s, e) => ExitApplication();
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    private void OnLockMenuClick(object? sender, EventArgs e)
    {
        if (_lockMenuItem == null) return;

        if (_lockMenuItem.Checked)
        {
            _inputMethodService.Lock();
            ShowNotification("输入法锁定", "已锁定为英文输入");
        }
        else
        {
            _inputMethodService.Unlock();
            ShowNotification("输入法锁定", "已解锁，恢复原输入源");
        }
    }

    private void OnAddLayoutClick(object? sender, EventArgs e)
    {
        var (success, message) = _layoutService.AddUSLayout();
        ShowNotification(success ? "成功" : "提示", message);
        UpdateAddLayoutMenuVisibility();
    }

    private void UpdateAddLayoutMenuVisibility()
    {
        if (_addLayoutMenuItem == null) return;

        var isAvailable = _layoutService.IsUSLayoutAvailable();
        _addLayoutMenuItem.Enabled = !isAvailable;
        _addLayoutMenuItem.Text = isAvailable 
            ? "美式键盘（US）已安装 ✓" 
            : "一键添加美式键盘（US）";
    }

    public void UpdateStatus(bool isLocked)
    {
        if (_lockMenuItem != null)
        {
            _lockMenuItem.Checked = isLocked;
        }

        _notifyIcon.Icon = isLocked ? _lockedIcon : _unlockedIcon;
        _notifyIcon.Text = isLocked 
            ? "KeyboardStop - 已锁定英文" 
            : "KeyboardStop - 已解锁";
    }

    public void ShowNotification(string title, string message)
    {
        _notifyIcon.ShowBalloonTip(2000, title, message, ToolTipIcon.Info);
    }

    private void ShowMainWindow()
    {
        _mainWindow?.ShowFromTray();
    }

    private void ExitApplication()
    {
        _notifyIcon.Visible = false;
        System.Windows.Application.Current.Shutdown();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _unlockedIcon?.Dispose();
        _lockedIcon?.Dispose();
        _disposed = true;
    }
}
