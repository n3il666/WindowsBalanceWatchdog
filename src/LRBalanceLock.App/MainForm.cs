using LRBalanceLock.App.Models;

namespace LRBalanceLock.App;

public sealed class MainForm : Form
{
    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly AudioDeviceService _devices;
    private readonly BalanceLockService _balance;
    private readonly StartupService _startup;
    private readonly TrayIconService _tray;
    private readonly ComboBox _deviceCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 420 };
    private readonly CheckBox _lockCheck = new() { Text = "Balance Lock", AutoSize = true };
    private readonly CheckBox _startupCheck = new() { Text = "Start with Windows", AutoSize = true };
    private readonly CheckBox _minimizeCheck = new() { Text = "Minimize to tray instead of close", AutoSize = true };
    private readonly Label _status = new() { AutoSize = true };
    private readonly Label _lastCorrection = new() { AutoSize = true };
    private readonly Label _channels = new() { AutoSize = true };
    private bool _allowExit;

    public MainForm(AppSettings settings, SettingsService settingsService, AudioDeviceService devices, BalanceLockService balance, StartupService startup, bool startMinimized)
    {
        _settings = settings; _settingsService = settingsService; _devices = devices; _balance = balance; _startup = startup;
        _tray = new TrayIconService(settings, ShowFromTray, ToggleLock, ToggleStartup, Quit);
        Text = "L/R Balance Lock"; Width = 560; Height = 370; StartPosition = FormStartPosition.CenterScreen;
        BuildUi();
        RefreshDevices();
        ApplySettingsToUi();
        _balance.StatusChanged += status => BeginInvoke((MethodInvoker)(() => UpdateStatus(status)));
        _balance.Start();
        if (startMinimized) Shown += (_, _) => Hide();
    }

    private void BuildUi()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 12, AutoSize = true };
        panel.Controls.Add(new Label { Text = "L/R Balance Lock", Font = new Font(Font, FontStyle.Bold), AutoSize = true });
        panel.Controls.Add(new Label { Text = "Keep your headset's left and right balance equal.", AutoSize = true });
        panel.Controls.Add(new Label { Text = "Device", AutoSize = true, Margin = new Padding(0, 16, 0, 0) });
        var deviceRow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        deviceRow.Controls.Add(_deviceCombo);
        deviceRow.Controls.Add(new Button { Text = "Refresh", AutoSize = true });
        ((Button)deviceRow.Controls[1]).Click += (_, _) => RefreshDevices();
        panel.Controls.Add(deviceRow);
        panel.Controls.Add(_status);
        panel.Controls.Add(_lockCheck);
        panel.Controls.Add(_startupCheck);
        panel.Controls.Add(_minimizeCheck);
        panel.Controls.Add(_lastCorrection);
        panel.Controls.Add(_channels);
        panel.Controls.Add(new Label { Text = "This app only keeps Windows left/right channel balance equal. It does not change spatial sound, EQ, device effects, or audio routing.", MaximumSize = new Size(500, 0), AutoSize = true, Margin = new Padding(0, 16, 0, 0) });
        var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        buttons.Controls.Add(new Button { Text = "Quit", AutoSize = true });
        buttons.Controls.Add(new Button { Text = "Minimize to Tray", AutoSize = true });
        ((Button)buttons.Controls[0]).Click += (_, _) => Quit();
        ((Button)buttons.Controls[1]).Click += (_, _) => Hide();
        panel.Controls.Add(buttons);
        Controls.Add(panel);

        _lockCheck.CheckedChanged += (_, _) => { _settings.BalanceLockEnabled = _lockCheck.Checked; SaveSettings(); };
        _startupCheck.CheckedChanged += (_, _) => { if (_startupCheck.Checked != _settings.StartWithWindows) ToggleStartup(); };
        _minimizeCheck.CheckedChanged += (_, _) => { _settings.MinimizeToTrayOnClose = _minimizeCheck.Checked; SaveSettings(); };
        _deviceCombo.SelectedIndexChanged += (_, _) => SaveSelectedDevice();
    }

    private void RefreshDevices()
    {
        var old = _deviceCombo.SelectedValue as string;
        _deviceCombo.DisplayMember = "FriendlyName"; _deviceCombo.ValueMember = "Id";
        var list = new List<AudioDeviceInfo> { new("default", "Default playback device", true) };
        list.AddRange(_devices.ListPlaybackDevices().Select(d => d with { IsDefault = false }));
        _deviceCombo.DataSource = list;
        _deviceCombo.SelectedValue = _settings.SelectedDeviceMode == "specific" ? _settings.SelectedDeviceId ?? old ?? "default" : "default";
    }

    private void ApplySettingsToUi()
    {
        _lockCheck.Checked = _settings.BalanceLockEnabled;
        _startupCheck.Checked = _settings.StartWithWindows;
        _minimizeCheck.Checked = _settings.MinimizeToTrayOnClose;
        UpdateStatus(_balance.CurrentStatus);
    }

    private void SaveSelectedDevice()
    {
        if (_deviceCombo.SelectedItem is not AudioDeviceInfo item) return;
        _settings.SelectedDeviceMode = item.Id == "default" ? "default" : "specific";
        _settings.SelectedDeviceId = item.Id == "default" ? null : item.Id;
        _settings.SelectedDeviceFriendlyName = item.FriendlyName;
        SaveSettings();
    }

    private void SaveSettings()
    {
        _settingsService.Save(_settings);
        _balance.UpdateSettings(_settings);
        _tray.Update(_settings, _balance.CurrentStatus);
    }

    private void UpdateStatus(BalanceStatus s)
    {
        _status.Text = $"Status: {s.State}";
        _lastCorrection.Text = $"Last correction: {(s.LastCorrection?.ToLocalTime().ToString("T") ?? "Never")}";
        _channels.Text = s.Left is null ? "Current channels: Unavailable" : $"Current channels: L {s.Left:P0} / R {s.Right:P0}";
        _tray.Update(_settings, s);
    }

    private void ToggleLock() { _lockCheck.Checked = !_lockCheck.Checked; }
    private void ToggleStartup()
    {
        try { _startup.SetEnabled(!_settings.StartWithWindows); _settings.StartWithWindows = !_settings.StartWithWindows; _startupCheck.Checked = _settings.StartWithWindows; SaveSettings(); }
        catch { MessageBox.Show("Could not change startup setting. Try running normally from your user account.", Text); _startupCheck.Checked = _settings.StartWithWindows; }
    }
    private void ShowFromTray() { Show(); WindowState = FormWindowState.Normal; Activate(); }
    private void Quit() { _allowExit = true; Close(); }
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_allowExit && _settings.MinimizeToTrayOnClose) { e.Cancel = true; Hide(); return; }
        _tray.Dispose(); _balance.Dispose(); base.OnFormClosing(e);
    }
}
