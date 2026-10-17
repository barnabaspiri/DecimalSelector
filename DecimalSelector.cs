// Decimal Selector - a system tray app that makes the NumPad decimal key type a comma or a dot.
// Build with build.cmd (uses the C# compiler that ships with Windows, no SDK needed).

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Decimal Selector")]
[assembly: AssemblyProduct("Decimal Selector")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

static class Program
{
    const string AppName = "Decimal Selector";
    const string SettingsKeyPath = @"Software\DecimalSelector";
    const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunValueName = "DecimalSelector";

    static char outputChar;
    static bool swallowing; // true while we are replacing a held NumPad decimal key
    static IntPtr hookHandle;
    static readonly LowLevelKeyboardProc hookProc = HookCallback; // kept in a field so the GC never collects it
    static NotifyIcon tray;
    static Icon commaIcon, dotIcon;
    static ToolStripMenuItem commaItem, dotItem, startupItem;

    [STAThread]
    static void Main()
    {
        bool isFirstInstance;
        using (new Mutex(true, "DecimalSelector.SingleInstance", out isFirstInstance))
        {
            if (!isFirstInstance)
            {
                MessageBox.Show(AppName + " is already running.\n\nLook for its icon in the system tray " +
                    "(you may need to click the ^ arrow on the taskbar).", AppName);
                return;
            }

            SetProcessDPIAware();
            Application.EnableVisualStyles();

            string saved = LoadSetting();
            outputChar = saved == "," ? ',' : '.';

            commaIcon = MakeIcon(",", Color.FromArgb(46, 158, 91));
            dotIcon = MakeIcon(".", Color.FromArgb(30, 102, 245));

            commaItem = new ToolStripMenuItem("Comma  ( , )", null, delegate { SetChar(','); });
            dotItem = new ToolStripMenuItem("Dot  ( . )", null, delegate { SetChar('.'); });
            startupItem = new ToolStripMenuItem("Start with Windows", null, delegate { ToggleStartup(); });
            startupItem.Checked = IsStartupEnabled();
            var exitItem = new ToolStripMenuItem("Exit", null, delegate { Application.Exit(); });

            var menu = new ContextMenuStrip();
            menu.Items.AddRange(new ToolStripItem[] {
                commaItem, dotItem, new ToolStripSeparator(), startupItem, new ToolStripSeparator(), exitItem });

            tray = new NotifyIcon { ContextMenuStrip = menu, Visible = true };
            tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) SetChar(outputChar == '.' ? ',' : '.'); };
            UpdateUi();

            hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, hookProc, GetModuleHandle(null), 0);
            if (hookHandle == IntPtr.Zero)
            {
                tray.Visible = false;
                MessageBox.Show("Could not install the keyboard hook.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (saved == null)
            {
                SetChar(outputChar); // remember the default so the welcome tip shows only once
                tray.ShowBalloonTip(5000, AppName,
                    "Running in the system tray. Click the icon to switch the NumPad key between comma and dot.",
                    ToolTipIcon.Info);
            }

            Application.Run();

            UnhookWindowsHookEx(hookHandle);
            tray.Visible = false;
        }
    }

    static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && Marshal.ReadInt32(lParam) == VK_DECIMAL) // first field of KBDLLHOOKSTRUCT is vkCode
        {
            int msg = wParam.ToInt32();
            bool keyDown = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
            // Leave Ctrl/Alt/Win shortcuts alone; once a press is being replaced, keep replacing its auto-repeats.
            if (keyDown && (swallowing || !ModifierDown()))
            {
                swallowing = true;
                TypeChar(outputChar);
                return (IntPtr)1;
            }
            if (!keyDown && swallowing)
            {
                swallowing = false;
                return (IntPtr)1;
            }
        }
        return CallNextHookEx(hookHandle, nCode, wParam, lParam);
    }

    static bool ModifierDown()
    {
        return IsDown(VK_CONTROL) || IsDown(VK_MENU) || IsDown(VK_LWIN) || IsDown(VK_RWIN);
    }

    static bool IsDown(int vk)
    {
        return (GetAsyncKeyState(vk) & 0x8000) != 0;
    }

    // Types the character as Unicode, so the result does not depend on the keyboard layout.
    static void TypeChar(char c)
    {
        var inputs = new INPUT[2];
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].u.ki.wScan = c;
        inputs[0].u.ki.dwFlags = KEYEVENTF_UNICODE;
        inputs[1] = inputs[0];
        inputs[1].u.ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;
        SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
    }

    static void SetChar(char c)
    {
        outputChar = c;
        using (var key = Registry.CurrentUser.CreateSubKey(SettingsKeyPath))
            key.SetValue("Char", c.ToString());
        UpdateUi();
    }

    static string LoadSetting()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(SettingsKeyPath))
            return key == null ? null : key.GetValue("Char") as string;
    }

    static void UpdateUi()
    {
        bool dot = outputChar == '.';
        commaItem.Checked = !dot;
        dotItem.Checked = dot;
        tray.Icon = dot ? dotIcon : commaIcon;
        tray.Text = AppName + " - NumPad types " + (dot ? "dot ( . )" : "comma ( , )");
    }

    static bool IsStartupEnabled()
    {
        using (var key = Registry.CurrentUser.OpenSubKey(RunKeyPath))
            return key != null && key.GetValue(RunValueName) != null;
    }

    static void ToggleStartup()
    {
        using (var key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
        {
            if (IsStartupEnabled()) key.DeleteValue(RunValueName, false);
            else key.SetValue(RunValueName, "\"" + Application.ExecutablePath + "\"");
        }
        startupItem.Checked = IsStartupEnabled();
    }

    // Draws a rounded square with the glyph in white, sized for the tray at the current DPI.
    static Icon MakeIcon(string glyph, Color background)
    {
        int size = SystemInformation.SmallIconSize.Width;
        using (var bmp = new Bitmap(size, size))
        using (var g = Graphics.FromImage(bmp))
        using (var family = new FontFamily("Georgia"))
        using (var back = new SolidBrush(background))
        using (var shape = new GraphicsPath())
        using (var reference = new GraphicsPath())
        using (var text = new GraphicsPath())
        using (var transform = new Matrix())
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float d = size * 0.45f;
            shape.AddArc(0, 0, d, d, 180, 90);
            shape.AddArc(size - d, 0, d, d, 270, 90);
            shape.AddArc(size - d, size - d, d, d, 0, 90);
            shape.AddArc(0, size - d, d, d, 90, 90);
            shape.CloseFigure();
            g.FillPath(back, shape);

            // Both glyphs are scaled relative to the comma's height, so the dot stays visibly smaller than the comma.
            reference.AddString(",", family, (int)FontStyle.Bold, 100, PointF.Empty, StringFormat.GenericTypographic);
            text.AddString(glyph, family, (int)FontStyle.Bold, 100, PointF.Empty, StringFormat.GenericTypographic);
            reference.Flatten(); // so GetBounds() is tight (it otherwise includes curve control points)
            text.Flatten();
            float scale = size * (glyph == "," ? 0.6f : 0.72f) / reference.GetBounds().Height;
            RectangleF b = text.GetBounds();
            transform.Translate(size / 2f, size / 2f);
            transform.Scale(scale, scale);
            transform.Translate(-(b.X + b.Width / 2), -(b.Y + b.Height / 2));
            text.Transform(transform);
            g.FillPath(Brushes.White, text);

            return Icon.FromHandle(bmp.GetHicon());
        }
    }

    // ---- Win32 interop ----

    const int WH_KEYBOARD_LL = 13;
    const int WM_KEYDOWN = 0x0100, WM_SYSKEYDOWN = 0x0104;
    const int VK_DECIMAL = 0x6E, VK_CONTROL = 0x11, VK_MENU = 0x12, VK_LWIN = 0x5B, VK_RWIN = 0x5C;
    const uint INPUT_KEYBOARD = 1, KEYEVENTF_KEYUP = 0x0002, KEYEVENTF_UNICODE = 0x0004;

    delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    struct INPUT { public uint type; public InputUnion u; }

    [StructLayout(LayoutKind.Explicit)]
    struct InputUnion { [FieldOffset(0)] public MOUSEINPUT mi; [FieldOffset(0)] public KEYBDINPUT ki; }

    [StructLayout(LayoutKind.Sequential)]
    struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }

    [StructLayout(LayoutKind.Sequential)]
    struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll", SetLastError = true)]
    static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();
}
