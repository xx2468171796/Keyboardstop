# KeyboardStop - 游戏输入法锁定工具

Windows 10/11 轻量常驻托盘程序，通过全局快捷键实现"强制英文输入"与"恢复原输入法"的快速切换。

## 功能特性

- 🎮 **全局快捷键** - 可自定义快捷键（默认 Ctrl+Alt+K）切换锁定状态
- 🔒 **一键锁定** - 锁定为 English (US) 输入，解锁时恢复原输入源
- 🔄 **纠偏轮询** - 可选功能，防止误触切换输入法（300ms 间隔检测）
- 📌 **托盘常驻** - 最小化到系统托盘，状态可视化
- ⌨️ **一键添加布局** - 未安装 US 键盘布局时可一键添加
- 💾 **配置持久化** - 设置保存在用户目录，支持开机自启

## 系统要求

- Windows 10/11 (x64)
- 普通用户权限（无需管理员）

## 快速开始

### 编译

```bash
cd src/KeyboardStop
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### 使用

1. 运行 `KeyboardStop.exe`，程序最小化到托盘
2. 按下 `Ctrl + Alt + K` 锁定英文输入
3. 再次按下解锁并恢复原输入源
4. 右键托盘图标可访问设置和其他功能

## 配置文件

配置保存在 `%AppData%\KeyboardStop\config.json`

```json
{
  "HotkeyModifiers": 3,      // Ctrl(2) + Alt(1) = 3
  "HotkeyKey": 75,           // K 键
  "EnableCorrectionPolling": true,
  "PollingIntervalMs": 300,
  "StartWithWindows": false,
  "StartLocked": false
}
```

## 技术栈

- C# .NET 8.0
- WPF (Windows Presentation Foundation)
- Windows User32 API (P/Invoke)

## 许可证

MIT License
