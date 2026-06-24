using LRBalanceLock.App.Models;

namespace LRBalanceLock.App;

public sealed class MainForm : Form
{
    private static readonly Color WindowBackground = Color.FromArgb(246, 248, 252);
    private static readonly Color CardBackground = Color.White;
    private static readonly Color Accent = Color.FromArgb(47, 107, 255);
    private static readonly Color TextPrimary = Color.FromArgb(28, 35, 48);
    private static readonly Color TextMuted = Color.FromArgb(94, 103, 120);
    private static readonly Color Border = Color.FromArgb(222, 228, 238);

    private readonly AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly AudioDeviceService _devices;
    private readonly BalanceLockService _balance;
    private readonly StartupService _startup;
    private readonly TrayIconService _tray;
    private readonly ComboBox _deviceCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
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
        Text = "L/R Balance Lock";
        Icon = AppIconFactory.AppIcon;
        MinimumSize = new Size(840, 640);
        ClientSize = new Size(860, 660);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = WindowBackground;
        DoubleBuffered = true;
        ResizeRedraw = true;
        Font = new Font("Segoe UI", 9.75f, FontStyle.Regular, GraphicsUnit.Point);
        BuildUi();
        RefreshDevices();
        ApplySettingsToUi();
        _balance.StatusChanged += OnBalanceStatusChanged;
        _balance.Start();
        if (startMinimized) Shown += (_, _) => Hide();
    }

    private void OnBalanceStatusChanged(BalanceStatus status)
    {
        if (IsDisposed) return;

        if (InvokeRequired)
        {
            if (!IsHandleCreated) return;
            BeginInvoke((MethodInvoker)(() => UpdateStatus(status)));
            return;
        }

        UpdateStatus(status);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(26), ColumnCount = 1, RowCount = 3 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 2, AutoSize = true, Margin = new Padding(0, 0, 0, 18) };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var iconBox = new PictureBox { Image = AppIconFactory.AppIcon.ToBitmap(), SizeMode = PictureBoxSizeMode.StretchImage, Size = new Size(48, 48), Margin = new Padding(0, 0, 14, 0) };
        var titleStack = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Dock = DockStyle.Fill };
        titleStack.Controls.Add(new Label { Text = "L/R Balance Lock", Font = new Font(Font.FontFamily, 18f, FontStyle.Bold), ForeColor = TextPrimary, AutoSize = true });
        titleStack.Controls.Add(new Label { Text = "Quietly keeps Windows headset balance centered.", ForeColor = TextMuted, AutoSize = true, Margin = new Padding(1, 2, 0, 0) });
        header.Controls.Add(iconBox, 0, 0);
        header.Controls.Add(titleStack, 1, 0);
        root.Controls.Add(header);

        var card = new Panel { Dock = DockStyle.Fill, BackColor = CardBackground, Padding = new Padding(22), Margin = new Padding(0) };
        card.Resize += (_, _) => card.Invalidate();
        card.Paint += (_, e) =>
        {
            var borderRectangle = card.ClientRectangle;
            borderRectangle.Width -= 1;
            borderRectangle.Height -= 1;
            using var borderPen = new Pen(Border);
            e.Graphics.DrawRectangle(borderPen, borderRectangle);
        };
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 12 };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label { Text = "Playback device", Font = new Font(Font, FontStyle.Bold), ForeColor = TextPrimary, AutoSize = true });
        panel.Controls.Add(new Label { Text = "Use your default output or pin balance locking to one device.", ForeColor = TextMuted, AutoSize = true, Margin = new Padding(0, 4, 0, 10) });

        var deviceRow = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 0, 0, 18) };
        deviceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        deviceRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        deviceRow.Controls.Add(_deviceCombo, 0, 0);
        deviceRow.Controls.Add(CreateButton("Refresh"), 1, 0);
        ((Button)deviceRow.GetControlFromPosition(1, 0)!).Click += (_, _) => RefreshDevices();
        panel.Controls.Add(deviceRow);

        StyleStatusLabel(_status, true);
        StyleStatusLabel(_channels, false);
        StyleStatusLabel(_lastCorrection, false);
        panel.Controls.Add(_status);
        panel.Controls.Add(_channels);
        panel.Controls.Add(_lastCorrection);

        var options = new GroupBox { Text = "Options", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(14), Margin = new Padding(0, 18, 0, 12), ForeColor = TextPrimary };
        var optionStack = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
        optionStack.Controls.Add(_lockCheck);
        optionStack.Controls.Add(_startupCheck);
        optionStack.Controls.Add(_minimizeCheck);
        options.Controls.Add(optionStack);
        panel.Controls.Add(options);

        card.Controls.Add(panel);
        root.Controls.Add(card);

        var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, Margin = new Padding(0, 18, 0, 0) };
        buttons.Controls.Add(CreateButton("Quit", true));
        buttons.Controls.Add(CreateButton("Minimize to Tray"));
        ((Button)buttons.Controls[0]).Click += (_, _) => Quit();
        ((Button)buttons.Controls[1]).Click += (_, _) => Hide();
        root.Controls.Add(buttons);
        Controls.Add(root);

        _lockCheck.CheckedChanged += (_, _) => { _settings.BalanceLockEnabled = _lockCheck.Checked; SaveSettings(); };
        _startupCheck.CheckedChanged += (_, _) => { if (_startupCheck.Checked != _settings.StartWithWindows) ToggleStartup(); };
        _minimizeCheck.CheckedChanged += (_, _) => { _settings.MinimizeToTrayOnClose = _minimizeCheck.Checked; SaveSettings(); };
        _deviceCombo.SelectedIndexChanged += (_, _) => SaveSelectedDevice();
    }

    private Button CreateButton(string text, bool primary = false)
    {
        return new Button
        {
            Text = text,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Accent : Color.White,
            ForeColor = primary ? Color.White : TextPrimary,
            Padding = new Padding(12, 7, 12, 7),
            Margin = new Padding(8, 0, 0, 0),
            MinimumSize = new Size(0, 36),
            UseVisualStyleBackColor = false
        };
    }

    private static void StyleStatusLabel(Label label, bool prominent)
    {
        label.AutoSize = true;
        label.Margin = new Padding(0, prominent ? 2 : 6, 0, 0);
        label.ForeColor = prominent ? Accent : TextMuted;
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
