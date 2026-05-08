using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenshotApp;

public class HotkeyManager : IDisposable
{
    public const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;

    private readonly IntPtr _handle;
    private readonly List<int> _ids = new();

    public HotkeyManager(IntPtr handle) => _handle = handle;

    public bool Register(int id, Keys key, bool ctrl = false, bool alt = false, bool shift = false)
    {
        uint mods = 0;
        if (ctrl) mods |= MOD_CONTROL;
        if (alt) mods |= MOD_ALT;
        if (shift) mods |= MOD_SHIFT;
        bool ok = RegisterHotKey(_handle, id, mods, (uint)key);
        if (ok) _ids.Add(id);
        return ok;
    }

    public void Dispose()
    {
        foreach (int id in _ids)
            UnregisterHotKey(_handle, id);
        _ids.Clear();
    }
}
