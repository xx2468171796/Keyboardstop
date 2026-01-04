using System.Windows.Threading;
using KeyboardStop.Native;

namespace KeyboardStop.Services;

public class InputMethodService
{
    private const string US_LAYOUT_ID = "00000409";
    private readonly IntPtr _usLayoutHandle;
    
    private IntPtr _previousLayout;
    private bool _isLocked;
    private DispatcherTimer? _correctionTimer;
    private int _pollingIntervalMs = 300;

    public bool IsLocked => _isLocked;

    public event Action<bool>? LockStateChanged;

    public InputMethodService()
    {
        _usLayoutHandle = new IntPtr(0x04090409);
    }

    public bool Lock()
    {
        try
        {
            var foregroundWindow = NativeMethods.GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero) return false;

            var threadId = NativeMethods.GetWindowThreadProcessId(foregroundWindow, out _);
            _previousLayout = NativeMethods.GetKeyboardLayout(threadId);

            // 尝试多种方式切换到 US 布局
            var success = SwitchToUSLayout(foregroundWindow, threadId);
            
            if (success)
            {
                _isLocked = true;
                LockStateChanged?.Invoke(true);
            }

            return success;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"锁定输入法失败: {ex.Message}");
            return false;
        }
    }

    public bool Unlock()
    {
        try
        {
            if (_previousLayout == IntPtr.Zero)
            {
                _isLocked = false;
                LockStateChanged?.Invoke(false);
                return true;
            }

            var foregroundWindow = NativeMethods.GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero) return false;

            // 恢复之前的布局
            NativeMethods.PostMessage(foregroundWindow, NativeMethods.WM_INPUTLANGCHANGEREQUEST, 
                IntPtr.Zero, _previousLayout);

            _isLocked = false;
            _previousLayout = IntPtr.Zero;
            LockStateChanged?.Invoke(false);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"解锁输入法失败: {ex.Message}");
            return false;
        }
    }

    private bool SwitchToUSLayout(IntPtr foregroundWindow, uint threadId)
    {
        // 方法1: 使用 PostMessage
        var result = NativeMethods.PostMessage(foregroundWindow, 
            NativeMethods.WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, _usLayoutHandle);

        if (!result)
        {
            // 方法2: 加载并激活布局
            var hkl = NativeMethods.LoadKeyboardLayout(US_LAYOUT_ID, 
                NativeMethods.KLF_ACTIVATE | NativeMethods.KLF_SETFORPROCESS);
            
            if (hkl != IntPtr.Zero)
            {
                // 附加到目标线程
                var currentThreadId = NativeMethods.GetCurrentThreadId();
                NativeMethods.AttachThreadInput(currentThreadId, threadId, true);
                NativeMethods.ActivateKeyboardLayout(hkl, NativeMethods.KLF_SETFORPROCESS);
                NativeMethods.AttachThreadInput(currentThreadId, threadId, false);
                result = true;
            }
        }

        return result;
    }

    public void StartCorrectionPolling(int intervalMs = 300)
    {
        _pollingIntervalMs = intervalMs;
        
        if (_correctionTimer != null)
        {
            _correctionTimer.Stop();
        }

        _correctionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(_pollingIntervalMs)
        };
        _correctionTimer.Tick += OnCorrectionTick;
        _correctionTimer.Start();
    }

    public void StopCorrectionPolling()
    {
        if (_correctionTimer != null)
        {
            _correctionTimer.Stop();
            _correctionTimer.Tick -= OnCorrectionTick;
            _correctionTimer = null;
        }
    }

    private void OnCorrectionTick(object? sender, EventArgs e)
    {
        if (!_isLocked) return;

        try
        {
            var foregroundWindow = NativeMethods.GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero) return;

            var threadId = NativeMethods.GetWindowThreadProcessId(foregroundWindow, out _);
            var currentLayout = NativeMethods.GetKeyboardLayout(threadId);

            // 检查是否为 US 布局（低16位为0x0409）
            var langId = currentLayout.ToInt64() & 0xFFFF;
            if (langId != 0x0409)
            {
                // 自动切回 US 布局
                SwitchToUSLayout(foregroundWindow, threadId);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"纠偏检测失败: {ex.Message}");
        }
    }

    public IntPtr GetCurrentLayout()
    {
        var foregroundWindow = NativeMethods.GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero) return IntPtr.Zero;

        var threadId = NativeMethods.GetWindowThreadProcessId(foregroundWindow, out _);
        return NativeMethods.GetKeyboardLayout(threadId);
    }
}
