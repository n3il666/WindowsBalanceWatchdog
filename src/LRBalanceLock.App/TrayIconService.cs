using LRBalanceLock.App.Models;

namespace LRBalanceLock.App;

public sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly ToolStripMenuItem _deviceItem;
    private readonly ToolStripMenuItem _startupItem;

    public TrayIconService(AppSettings settings, Action open, Action toggleLock, Action toggleStartup, Action quit)
    {
        _toggleItem = new ToolStripMenuItem("Balance Lock: Off", null, (_, _) => toggleLock());
        _deviceItem = new ToolStripMenuItem("Device: Default playback device") { Enabled = false };
        _startupItem = new ToolStripMenuItem("Start with Windows: Off", null, (_, _) => toggleStartup());
        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripLabel("L/R Balance Lock"));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_toggleItem);
        menu.Items.Add(_deviceItem);
        menu.Items.Add(new ToolStripMenuItem("Open", null, (_, _) => open()));
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripMenuItem("Quit", null, (_, _) => quit()));

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "L/R Balance Lock: Off",
            Visible = true,
            ContextMenuStrip = menu
        };
        _notifyIcon.DoubleClick += (_, _) => open();
        Update(settings, null);
    }

    public void Update(AppSettings settings, BalanceStatus? status)
    {
        _toggleItem.Text = $"Balance Lock: {(settings.BalanceLockEnabled ? "On" : "Off")}";
        _startupItem.Text = $"Start with Windows: {(settings.StartWithWindows ? "On" : "Off")}";
        _deviceItem.Text = $"Device: {status?.DeviceName ?? settings.SelectedDeviceFriendlyName ?? "Default playback device"}";
        _notifyIcon.Text = $"L/R Balance Lock: {(settings.BalanceLockEnabled ? "On" : "Off")}";
    }

    public void Dispose() => _notifyIcon.Dispose();
}
