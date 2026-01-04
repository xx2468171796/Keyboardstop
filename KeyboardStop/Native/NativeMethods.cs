using System.Runtime.InteropServices;

namespace KeyboardStop.Native;

internal static class NativeMethods
{
    public const int WM_INPUTLANGCHANGEREQUEST = 0x0050;
    public const int WM_HOTKEY = 0x0312;
    public const int HWND_BROADCAST = 0xFFFF;
    
    public const uint KLF_ACTIVATE = 0x00000001;
    public const uint KLF_SETFORPROCESS = 0x00000100;
    
    public const int MOD_ALT = 0x0001;
    public const int MOD_CONTROL = 0x0002;
    public const int MOD_SHIFT = 0x0004;
    public const int MOD_WIN = 0x0008;
    public const int MOD_NOREPEAT = 0x4000;

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    public static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint flags);

    [DllImport("user32.dll")]
    public static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint flags);

    [DllImport("user32.dll")]
    public static extern bool UnloadKeyboardLayout(IntPtr hkl);

    [DllImport("user32.dll")]
    public static extern uint GetKeyboardLayoutList(int nBuff, [Out] IntPtr[]? lpList);

    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();
}
