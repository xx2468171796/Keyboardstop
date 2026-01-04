using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace KeyboardStop.Resources;

public static class Icons
{
    public static Icon CreateTrayIcon(bool isLocked)
    {
        var size = 32;
        using var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);
        
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        g.Clear(Color.Transparent);

        // 背景圆形
        var bgColor = isLocked ? Color.FromArgb(33, 150, 243) : Color.FromArgb(128, 128, 128);
        using var bgBrush = new SolidBrush(bgColor);
        g.FillEllipse(bgBrush, 1, 1, size - 2, size - 2);

        // 文字 EN 或 中
        var text = isLocked ? "EN" : "中";
        using var font = new Font("Microsoft YaHei", isLocked ? 10 : 14, FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.White);
        
        var textSize = g.MeasureString(text, font);
        var x = (size - textSize.Width) / 2;
        var y = (size - textSize.Height) / 2;
        g.DrawString(text, font, textBrush, x, y);

        return Icon.FromHandle(bitmap.GetHicon());
    }

    public static Icon CreateDefaultIcon()
    {
        return CreateTrayIcon(false);
    }

    public static Icon CreateLockedIcon()
    {
        return CreateTrayIcon(true);
    }

    public static void SaveAppIcon(string path)
    {
        var sizes = new[] { 16, 32, 48, 256 };
        using var ms = new MemoryStream();
        
        // ICO header
        ms.Write(new byte[] { 0, 0 }, 0, 2); // Reserved
        ms.Write(new byte[] { 1, 0 }, 0, 2); // Type (1 = ICO)
        ms.Write(BitConverter.GetBytes((short)sizes.Length), 0, 2); // Count
        
        var imageDataList = new List<byte[]>();
        var offset = 6 + (sizes.Length * 16); // Header + entries
        
        foreach (var size in sizes)
        {
            var imageData = CreateIconImageData(size);
            imageDataList.Add(imageData);
            
            // ICO directory entry
            ms.WriteByte((byte)(size == 256 ? 0 : size)); // Width
            ms.WriteByte((byte)(size == 256 ? 0 : size)); // Height
            ms.WriteByte(0); // Color palette
            ms.WriteByte(0); // Reserved
            ms.Write(new byte[] { 1, 0 }, 0, 2); // Color planes
            ms.Write(new byte[] { 32, 0 }, 0, 2); // Bits per pixel
            ms.Write(BitConverter.GetBytes(imageData.Length), 0, 4); // Size
            ms.Write(BitConverter.GetBytes(offset), 0, 4); // Offset
            
            offset += imageData.Length;
        }
        
        foreach (var data in imageDataList)
        {
            ms.Write(data, 0, data.Length);
        }
        
        File.WriteAllBytes(path, ms.ToArray());
    }

    private static byte[] CreateIconImageData(int size)
    {
        using var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);
        
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        g.Clear(Color.Transparent);

        // 背景渐变圆形
        using var bgBrush = new LinearGradientBrush(
            new Rectangle(0, 0, size, size),
            Color.FromArgb(33, 150, 243),
            Color.FromArgb(21, 101, 192),
            45f);
        g.FillEllipse(bgBrush, 1, 1, size - 2, size - 2);

        // 游戏手柄图案
        var scale = size / 32f;
        var controllerWidth = 20 * scale;
        var controllerHeight = 12 * scale;
        var cx = (size - controllerWidth) / 2;
        var cy = (size - controllerHeight) / 2;

        using var controllerBrush = new SolidBrush(Color.White);
        
        // 手柄主体
        g.FillEllipse(controllerBrush, cx, cy, controllerWidth, controllerHeight);
        
        // 左右握把
        var gripSize = 6 * scale;
        g.FillEllipse(controllerBrush, cx - gripSize/3, cy + controllerHeight/4, gripSize, gripSize * 1.2f);
        g.FillEllipse(controllerBrush, cx + controllerWidth - gripSize/1.5f, cy + controllerHeight/4, gripSize, gripSize * 1.2f);

        // 按钮
        using var buttonBrush = new SolidBrush(Color.FromArgb(33, 150, 243));
        var btnSize = 2.5f * scale;
        g.FillEllipse(buttonBrush, cx + controllerWidth * 0.65f, cy + controllerHeight * 0.3f, btnSize, btnSize);
        g.FillEllipse(buttonBrush, cx + controllerWidth * 0.75f, cy + controllerHeight * 0.45f, btnSize, btnSize);

        using var ms = new MemoryStream();
        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return ms.ToArray();
    }
}
