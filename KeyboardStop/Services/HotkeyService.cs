using System.Windows.Forms;
using KeyboardStop.Native;

namespace KeyboardStop.Services;

public class HotkeyService : IDisposable
{
    private readonly Dictionary<int, Action> _hotkeyActions = new();
    private HotkeyWindow? _hotkeyWindow;
    private int _currentId = 0;
    private bool _disposed;

    public event Action<string>? HotkeyRegistrationFailed;
    public event Action? HotkeyTriggered;

    public bool Register(int modifiers, int key, Action callback)
    {
        if (_hotkeyWindow == null)
        {
            _hotkeyWindow = new HotkeyWindow();
            _hotkeyWindow.HotkeyPressed += OnHotkeyPressed;
        }

        var id = ++_currentId;
        
        var success = NativeMethods.RegisterHotKey(_hotkeyWindow.Handle, id, 
            modifiers | NativeMethods.MOD_NOREPEAT, key);

        if (success)
        {
            _hotkeyActions[id] = callback;
            return true;
        }
        else
        {
            HotkeyRegistrationFailed?.Invoke("快捷键注册失败：该组合键可能被系统或其他程序占用，请更换。");
            return false;
        }
    }

    private void OnHotkeyPressed(int id)
    {
        if (_hotkeyActions.TryGetValue(id, out var action))
        {
            action.Invoke();
        }
        HotkeyTriggered?.Invoke();
    }

    public void Unregister(int id)
    {
        if (_hotkeyActions.ContainsKey(id) && _hotkeyWindow != null)
        {
            NativeMethods.UnregisterHotKey(_hotkeyWindow.Handle, id);
            _hotkeyActions.Remove(id);
        }
    }

    public void UnregisterAll()
    {
        if (_hotkeyWindow == null) return;
        
        foreach (var id in _hotkeyActions.Keys.ToList())
        {
            NativeMethods.UnregisterHotKey(_hotkeyWindow.Handle, id);
        }
        _hotkeyActions.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        UnregisterAll();
        _hotkeyWindow?.DestroyHandle();
        _disposed = true;
    }

    private class HotkeyWindow : NativeWindow
    {
        public event Action<int>? HotkeyPressed;

        public HotkeyWindow()
        {
            CreateHandle(new CreateParams
            {
                Caption = "KeyboardStopHotkeyWindow",
                Style = 0,
                ExStyle = 0,
                Parent = IntPtr.Zero
            });
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY)
            {
                var id = m.WParam.ToInt32();
                HotkeyPressed?.Invoke(id);
            }
            base.WndProc(ref m);
        }
    }
}
