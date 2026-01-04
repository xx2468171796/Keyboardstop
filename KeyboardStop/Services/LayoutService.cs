using KeyboardStop.Native;
using Microsoft.Win32;

namespace KeyboardStop.Services;

public class LayoutService
{
    private const string US_LAYOUT_ID = "00000409";
    private const int US_LANG_ID = 0x0409;

    public bool IsUSLayoutAvailable()
    {
        try
        {
            var count = NativeMethods.GetKeyboardLayoutList(0, null);
            if (count == 0) return false;

            var layouts = new IntPtr[count];
            NativeMethods.GetKeyboardLayoutList((int)count, layouts);

            foreach (var layout in layouts)
            {
                var langId = layout.ToInt64() & 0xFFFF;
                if (langId == US_LANG_ID)
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public (bool success, string message) AddUSLayout()
    {
        try
        {
            // 方法1: 直接加载键盘布局（立即可用）
            var hkl = NativeMethods.LoadKeyboardLayout(US_LAYOUT_ID, 
                NativeMethods.KLF_ACTIVATE);

            if (hkl == IntPtr.Zero)
            {
                return (false, "添加 English (US) 键盘布局失败：可能被系统策略限制。你仍可在 Windows 设置中手动添加语言/键盘。");
            }

            // 方法2: 写入注册表使其持久化
            var persistSuccess = PersistLayoutToRegistry();

            if (persistSuccess)
            {
                // 广播设置变化
                BroadcastSettingChange();
                return (true, "已添加 English (US) 键盘布局");
            }
            else
            {
                return (true, "已可立即使用，但可能需注销后出现在切换列表中");
            }
        }
        catch (Exception ex)
        {
            return (false, $"添加键盘布局失败: {ex.Message}");
        }
    }

    private bool PersistLayoutToRegistry()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Keyboard Layout\Preload", true);
            if (key == null) return false;

            // 获取现有布局数量
            var valueNames = key.GetValueNames();
            var nextIndex = valueNames.Length + 1;

            // 检查是否已存在
            foreach (var name in valueNames)
            {
                var value = key.GetValue(name)?.ToString();
                if (value == US_LAYOUT_ID)
                {
                    return true; // 已存在
                }
            }

            // 添加新布局
            key.SetValue(nextIndex.ToString(), US_LAYOUT_ID);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void BroadcastSettingChange()
    {
        try
        {
            // 广播 WM_INPUTLANGCHANGEREQUEST 到所有窗口
            NativeMethods.PostMessage(new IntPtr(NativeMethods.HWND_BROADCAST),
                NativeMethods.WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, IntPtr.Zero);
        }
        catch
        {
            // 忽略广播失败
        }
    }

    public List<(IntPtr handle, string name)> GetAvailableLayouts()
    {
        var result = new List<(IntPtr, string)>();

        try
        {
            var count = NativeMethods.GetKeyboardLayoutList(0, null);
            if (count == 0) return result;

            var layouts = new IntPtr[count];
            NativeMethods.GetKeyboardLayoutList((int)count, layouts);

            foreach (var layout in layouts)
            {
                var langId = (int)(layout.ToInt64() & 0xFFFF);
                var name = GetLayoutName(langId);
                result.Add((layout, name));
            }
        }
        catch
        {
            // 返回空列表
        }

        return result;
    }

    private string GetLayoutName(int langId)
    {
        return langId switch
        {
            0x0409 => "English (US)",
            0x0804 => "中文 (简体)",
            0x0404 => "中文 (繁体)",
            0x0411 => "日语",
            0x0412 => "韩语",
            _ => $"布局 0x{langId:X4}"
        };
    }
}
