using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ScreenshotApp;

public class WindowPickerDialog : Form
{
    public IntPtr SelectedHandle { get; private set; }

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private readonly ListBox _list = new();
    private readonly List<IntPtr> _handles = new();

    public WindowPickerDialog()
    {
        Text = "Seleziona finestra";
        Size = new Size(420, 380);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        var label = new Label
        {
            Text = "Seleziona la finestra da catturare:",
            Dock = DockStyle.Top,
            Height = 28,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(4, 0, 0, 0)
        };

        _list.Dock = DockStyle.Fill;
        _list.Font = new Font("Segoe UI", 9f);
        _list.DoubleClick += (_, _) => Pick();

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(5)
        };

        var btnCapture = new Button { Text = "Cattura", Width = 80, Height = 30, DialogResult = DialogResult.None };
        var btnCancel = new Button { Text = "Annulla", Width = 80, Height = 30, DialogResult = DialogResult.Cancel };
        btnCapture.Click += (_, _) => Pick();

        btnPanel.Controls.Add(btnCancel);
        btnPanel.Controls.Add(btnCapture);

        Controls.Add(_list);
        Controls.Add(btnPanel);
        Controls.Add(label);

        LoadWindows();
    }

    private void LoadWindows()
    {
        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd)) return true;
            if (GetParent(hWnd) != IntPtr.Zero) return true;
            int len = GetWindowTextLength(hWnd);
            if (len == 0) return true;
            var sb = new StringBuilder(len + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            string title = sb.ToString().Trim();
            if (!string.IsNullOrEmpty(title) && hWnd != Handle)
            {
                _handles.Add(hWnd);
                _list.Items.Add(title);
            }
            return true;
        }, IntPtr.Zero);
    }

    private void Pick()
    {
        if (_list.SelectedIndex < 0) return;
        SelectedHandle = _handles[_list.SelectedIndex];
        DialogResult = DialogResult.OK;
        Close();
    }
}
