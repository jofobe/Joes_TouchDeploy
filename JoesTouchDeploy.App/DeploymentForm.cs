using JoesTouchDeploy.Core;
using JoesTouchDeploy.Core.Logging;
using JoesTouchDeploy.Core.Models;
using JoesTouchDeploy.Core.Services;

namespace JoesTouchDeploy.App;

public class DeploymentForm : Form
{
    private static readonly Color MainBackground = Color.FromArgb(0x1E, 0x1E, 0x1E);
    private static readonly Color PanelBackground = Color.FromArgb(0x25, 0x25, 0x26);
    private static readonly Color InputBackground = Color.FromArgb(0x33, 0x33, 0x33);
    private static readonly Color ButtonBackground = Color.FromArgb(0x3C, 0x3C, 0x3C);
    private static readonly Color BorderColor = Color.FromArgb(0x4A, 0x4A, 0x4A);
    private static readonly Color PrimaryText = Color.FromArgb(0xF0, 0xF0, 0xF0);
    private static readonly Color SecondaryText = Color.FromArgb(0xC8, 0xC8, 0xC8);
    private static readonly Color AccentColor = Color.FromArgb(0x00, 0x7A, 0xCC);
    private static readonly Color SuccessColor = Color.FromArgb(0x4C, 0xAF, 0x50);
    private static readonly Color WarningColor = Color.FromArgb(0xFF, 0x98, 0x00);
    private static readonly Color ErrorColor = Color.FromArgb(0xF4, 0x43, 0x36);

    private readonly TextBox _friendlyNameTextBox = new();
    private readonly ComboBox _profileComboBox = new();
    private readonly TextBox _usernameTextBox = new();
    private readonly TextBox _passwordTextBox = new();
    private readonly Button _connectButton = new();
    private readonly Button _saveProfileButton = new();
    private readonly Button _deleteProfileButton = new();
    private readonly Label _modelValueLabel = new();
    private readonly Label _currentProjectValueLabel = new();
    private readonly Button _browseButton = new();
    private readonly Label _selectedProjectFileNameLabel = new();
    private readonly Button _deployButton = new();
    private readonly TextBox _statusTextBox = new();
    private readonly ProgressBar _progressBar = new();
    private readonly ToolTip _toolTip = new();
    private readonly FileDialogService _fileDialogService = new();
    private readonly ProfileService _profileService = new();
    private readonly List<ConnectionProfile> _profiles = [];

    private VideoTecClient? _videoTecClient;
    private DeploymentService? _deploymentService;
    private string? _selectedProjectFile;

    public DeploymentForm()
    {
        Text = "Joe's Touch Deploy";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 850);
        Size = new Size(1100, 850);
        BackColor = MainBackground;
        ForeColor = PrimaryText;

        InitializeControls();
        Load += DeploymentForm_Load;
    }

    private void InitializeControls()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(22),
            BackColor = MainBackground
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 66));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 34));

        var title = new Label
        {
            Text = "Joe's Touch Deploy",
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font(Font.FontFamily, 22, FontStyle.Bold),
            ForeColor = PrimaryText,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var topLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = MainBackground,
            Margin = new Padding(0, 0, 0, 18)
        };

        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        topLayout.Controls.Add(CreatePanelInformationSection(), 0, 0);
        topLayout.Controls.Add(CreateProjectSection(), 1, 0);

        root.Controls.Add(title, 0, 0);
        root.Controls.Add(topLayout, 0, 1);
        root.Controls.Add(CreateStatusSection(), 0, 2);

        Controls.Add(root);
    }

    private Control CreatePanelInformationSection()
    {
        var section = CreateSectionPanel("Panel Information");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            BackColor = PanelBackground,
            Padding = new Padding(18, 12, 18, 18)
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        ConfigureInput(_friendlyNameTextBox);
        _friendlyNameTextBox.Font = new Font(Font.FontFamily, 14, FontStyle.Bold);
        _friendlyNameTextBox.PlaceholderText = "Friendly name";

        ConfigureComboBox(_profileComboBox);
        _profileComboBox.Text = "10.0.0.29";
        _profileComboBox.DropDownStyle = ComboBoxStyle.DropDown;
        _profileComboBox.SelectedIndexChanged += ProfileComboBox_SelectedIndexChanged;

        ConfigureInput(_usernameTextBox);
        ConfigureInput(_passwordTextBox);
        _usernameTextBox.Text = "admin";
        _passwordTextBox.UseSystemPasswordChar = true;

        ConfigureValueLabel(_modelValueLabel, "-----------------");
        ConfigureValueLabel(_currentProjectValueLabel, "-----------------");

        ConfigureButton(_connectButton, "Connect", isPrimary: false);
        _connectButton.Click += ConnectButton_Click;

        ConfigureButton(_saveProfileButton, "Save Profile", isPrimary: false, deEmphasized: true);
        _saveProfileButton.Click += SaveProfileButton_Click;

        ConfigureButton(_deleteProfileButton, "Delete Profile", isPrimary: false, deEmphasized: true);
        _deleteProfileButton.Click += DeleteProfileButton_Click;

        layout.Controls.Add(_friendlyNameTextBox, 0, 0);
        layout.SetColumnSpan(_friendlyNameTextBox, 2);

        var ipSecondaryLabel = new Label
        {
            Text = "IP Address",
            Dock = DockStyle.Fill,
            ForeColor = SecondaryText,
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(ipSecondaryLabel, 0, 1);
        layout.SetColumnSpan(ipSecondaryLabel, 2);

        AddLabel(layout, "IP Address", 0, 2);
        layout.Controls.Add(_profileComboBox, 1, 2);

        AddLabel(layout, "Username", 0, 3);
        layout.Controls.Add(_usernameTextBox, 1, 3);

        AddLabel(layout, "Password", 0, 4);
        layout.Controls.Add(_passwordTextBox, 1, 4);

        AddLabel(layout, "Model", 0, 5);
        layout.Controls.Add(_modelValueLabel, 1, 5);

        AddLabel(layout, "Current Project", 0, 6);
        layout.Controls.Add(_currentProjectValueLabel, 1, 6);

        layout.Controls.Add(CreatePanelButtonRow(), 0, 7);
        layout.SetColumnSpan(layout.GetControlFromPosition(0, 7), 2);

        section.Controls.Add(layout);
        return section;
    }

    private Control CreatePanelButtonRow()
    {
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = PanelBackground,
            Padding = new Padding(0, 10, 0, 0)
        };

        row.Controls.Add(_connectButton);
        row.Controls.Add(_saveProfileButton);
        row.Controls.Add(_deleteProfileButton);

        return row;
    }

    private Control CreateProjectSection()
    {
        var section = CreateSectionPanel("Project");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = PanelBackground,
            Padding = new Padding(22)
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var selectedProjectCaption = new Label
        {
            Text = "Selected project",
            Dock = DockStyle.Fill,
            ForeColor = SecondaryText,
            TextAlign = ContentAlignment.BottomLeft
        };

        _selectedProjectFileNameLabel.Text = "No project selected.";
        _selectedProjectFileNameLabel.Font = new Font(Font.FontFamily, 16, FontStyle.Bold);
        _selectedProjectFileNameLabel.ForeColor = PrimaryText;
        _selectedProjectFileNameLabel.Dock = DockStyle.Fill;
        _selectedProjectFileNameLabel.AutoEllipsis = true;
        _selectedProjectFileNameLabel.TextAlign = ContentAlignment.TopLeft;

        ConfigureButton(_browseButton, "Browse...", isPrimary: false);
        _browseButton.Width = 160;
        _browseButton.Height = 40;
        _browseButton.Click += BrowseButton_Click;

        ConfigureButton(_deployButton, "Deploy Project", isPrimary: true);
        _deployButton.Enabled = false;
        _deployButton.Width = 260;
        _deployButton.Height = 58;
        _deployButton.Font = new Font(Font.FontFamily, 13, FontStyle.Bold);
        _deployButton.Click += DeployButton_Click;

        layout.Controls.Add(selectedProjectCaption, 0, 0);
        layout.Controls.Add(_selectedProjectFileNameLabel, 0, 1);
        layout.Controls.Add(CreateLeftAlignedButtonPanel(_browseButton), 0, 2);
        layout.Controls.Add(CreateCenteredButtonPanel(_deployButton), 0, 3);

        section.Controls.Add(layout);
        return section;
    }

    private Control CreateStatusSection()
    {
        var section = CreateSectionPanel("Deployment Status");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = PanelBackground,
            Padding = new Padding(14)
        };

        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _progressBar.Dock = DockStyle.Fill;
        _progressBar.Style = ProgressBarStyle.Marquee;
        _progressBar.MarqueeAnimationSpeed = 0;
        _progressBar.Visible = false;

        _statusTextBox.Multiline = true;
        _statusTextBox.ReadOnly = true;
        _statusTextBox.ScrollBars = ScrollBars.Vertical;
        _statusTextBox.Dock = DockStyle.Fill;
        _statusTextBox.Text = "Ready.";
        _statusTextBox.Font = new Font(FontFamily.GenericMonospace, 9);
        _statusTextBox.BackColor = InputBackground;
        _statusTextBox.ForeColor = PrimaryText;
        _statusTextBox.BorderStyle = BorderStyle.FixedSingle;

        layout.Controls.Add(_progressBar, 0, 0);
        layout.Controls.Add(_statusTextBox, 0, 1);

        section.Controls.Add(layout);
        return section;
    }

    private async void ConnectButton_Click(object? sender, EventArgs e)
    {
        await RunUiOperationAsync(async () =>
        {
            SetBusy(true);
            SetStatus("? Connecting to panel...");

            var debugOutputDirectory = Path.Combine(AppContext.BaseDirectory, "DebugOutput");
            var logger = new DebugLogger(debugOutputDirectory);
            var ipAddress = GetCurrentIpAddress();
            var connection = new PanelConnection
            {
                IpAddress = ipAddress,
                Username = _usernameTextBox.Text.Trim(),
                Password = _passwordTextBox.Text
            };

            _videoTecClient = new VideoTecClient(connection, logger);
            _deploymentService = new DeploymentService(_videoTecClient, logger);

            await _videoTecClient.LoginAsync();
            AppendSuccess("Connected and authenticated.");

            var deviceInformationResult = await _videoTecClient.GetDeviceInformationAsync();
            if (!deviceInformationResult.Success || deviceInformationResult.Data == null)
            {
                AppendError($"Device information unavailable: {deviceInformationResult.Message}");
                return;
            }

            var currentProjectResult = await _videoTecClient.GetCurrentProjectInformationAsync();
            if (!currentProjectResult.Success || currentProjectResult.Data == null)
            {
                AppendError($"Current project unavailable: {currentProjectResult.Message}");
                return;
            }

            _modelValueLabel.Text = deviceInformationResult.Data.Model;
            _currentProjectValueLabel.Text = currentProjectResult.Data.ProjectName;
            _toolTip.SetToolTip(_currentProjectValueLabel, currentProjectResult.Data.ProjectName);

            AppendSuccess("Panel information loaded.");
            await SaveOrUpdateProfileAfterConnectAsync(ipAddress, connection.Username);
            UpdateDeployButtonState();
        });
    }

    private async void DeploymentForm_Load(object? sender, EventArgs e)
    {
        await LoadProfilesAsync();
    }

    private void ProfileComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_profileComboBox.SelectedItem is not ConnectionProfile profile)
        {
            return;
        }

        _profileComboBox.Text = profile.IpAddress;
        _friendlyNameTextBox.Text = profile.FriendlyName ?? string.Empty;
        _usernameTextBox.Text = profile.Username;
    }

    private async void SaveProfileButton_Click(object? sender, EventArgs e)
    {
        await SaveCurrentProfileAsync();
    }

    private async void DeleteProfileButton_Click(object? sender, EventArgs e)
    {
        var ipAddress = GetCurrentIpAddress();
        var existingProfile = ProfileService.FindByIpAddress(_profiles, ipAddress);

        if (existingProfile == null)
        {
            AppendWarning("No saved profile exists for this IP address.");
            return;
        }

        var result = MessageBox.Show(
            this,
            $"Delete profile for {existingProfile}?",
            "Delete Profile",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            return;
        }

        _profiles.Remove(existingProfile);
        await _profileService.SaveProfilesAsync(_profiles);
        BindProfiles(ipAddress);
        AppendSuccess($"Profile deleted: {existingProfile}");
    }

    private void BrowseButton_Click(object? sender, EventArgs e)
    {
        var selectedFile = _fileDialogService.SelectVtzProjectFile(this);
        if (string.IsNullOrWhiteSpace(selectedFile))
        {
            AppendWarning("Project selection canceled.");
            return;
        }

        _selectedProjectFile = selectedFile;
        _selectedProjectFileNameLabel.Text = Path.GetFileName(selectedFile);
        _toolTip.SetToolTip(_selectedProjectFileNameLabel, selectedFile);
        AppendSuccess($"Project selected: {Path.GetFileName(selectedFile)}");
        UpdateDeployButtonState();
    }

    private async Task LoadProfilesAsync()
    {
        _profiles.Clear();
        _profiles.AddRange(await _profileService.LoadProfilesAsync());
        BindProfiles(_profileComboBox.Text);
        AppendStatus($"? Loaded {_profiles.Count} connection profile(s).");
    }

    private void BindProfiles(string? currentText = null)
    {
        _profileComboBox.SelectedIndexChanged -= ProfileComboBox_SelectedIndexChanged;
        _profileComboBox.Items.Clear();

        foreach (var profile in _profiles)
        {
            _profileComboBox.Items.Add(profile);
        }

        _profileComboBox.Text = currentText ?? string.Empty;
        _profileComboBox.SelectedIndexChanged += ProfileComboBox_SelectedIndexChanged;
    }

    private async Task SaveOrUpdateProfileAfterConnectAsync(string ipAddress, string username)
    {
        var existingProfile = ProfileService.FindByIpAddress(_profiles, ipAddress);

        if (existingProfile != null)
        {
            var friendlyName = _friendlyNameTextBox.Text.Trim();
            var updated = false;

            if (!existingProfile.Username.Equals(username, StringComparison.Ordinal))
            {
                existingProfile.Username = username;
                updated = true;
            }

            if (!string.Equals(existingProfile.FriendlyName ?? string.Empty, friendlyName, StringComparison.Ordinal))
            {
                existingProfile.FriendlyName = string.IsNullOrWhiteSpace(friendlyName) ? null : friendlyName;
                updated = true;
            }

            if (updated)
            {
                await _profileService.SaveProfilesAsync(_profiles);
                BindProfiles(ipAddress);
                AppendSuccess("Profile updated.");
            }

            return;
        }

        var result = MessageBox.Show(
            this,
            "Save this panel as a connection profile?",
            "Save Profile",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            await SaveCurrentProfileAsync();
        }
    }

    private async Task SaveCurrentProfileAsync()
    {
        var ipAddress = GetCurrentIpAddress();
        var username = _usernameTextBox.Text.Trim();
        var friendlyName = _friendlyNameTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            AppendWarning("Enter an IP address before saving a profile.");
            return;
        }

        var existingProfile = ProfileService.FindByIpAddress(_profiles, ipAddress);

        if (existingProfile == null)
        {
            _profiles.Add(new ConnectionProfile
            {
                FriendlyName = string.IsNullOrWhiteSpace(friendlyName) ? null : friendlyName,
                IpAddress = ipAddress,
                Username = username
            });
        }
        else
        {
            existingProfile.FriendlyName = string.IsNullOrWhiteSpace(friendlyName) ? null : friendlyName;
            existingProfile.IpAddress = ipAddress;
            existingProfile.Username = username;
        }

        await _profileService.SaveProfilesAsync(_profiles);
        BindProfiles(ipAddress);
        AppendSuccess($"Profile saved for {ipAddress}.");
    }

    private string GetCurrentIpAddress()
    {
        return (_profileComboBox.SelectedItem as ConnectionProfile)?.IpAddress ?? _profileComboBox.Text.Trim();
    }

    private async void DeployButton_Click(object? sender, EventArgs e)
    {
        if (_deploymentService == null || string.IsNullOrWhiteSpace(_selectedProjectFile))
        {
            AppendWarning("Connect to a panel and select a project before deploying.");
            return;
        }

        await RunUiOperationAsync(async () =>
        {
            SetBusy(true);
            SetStatus("? Deploying project...");
            AppendStatus("? Uploading project...");

            var deploymentResult = await _deploymentService.DeployProjectAsync(_selectedProjectFile);

            if (deploymentResult.UploadSucceeded)
            {
                AppendSuccess("Upload complete.");
            }
            else
            {
                AppendError("Upload failed.");
            }

            AppendStatus("? Waiting for panel UI...");

            if (deploymentResult.UiResponsive)
            {
                AppendSuccess("Panel UI is responding.");
            }
            else
            {
                AppendError("Panel UI did not respond before timeout.");
            }

            if (deploymentResult.Success)
            {
                AppendSuccess("Deployment complete.");
            }
            else
            {
                AppendError($"Deployment failed: {deploymentResult.Message}");
            }

            if (deploymentResult.CurrentProjectInformation != null)
            {
                _currentProjectValueLabel.Text = deploymentResult.CurrentProjectInformation.ProjectName;
                _toolTip.SetToolTip(_currentProjectValueLabel, deploymentResult.CurrentProjectInformation.ProjectName);
                AppendSuccess($"Current project: {deploymentResult.CurrentProjectInformation.ProjectName}");
            }
        });
    }

    private async Task RunUiOperationAsync(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (Exception exception)
        {
            AppendError(exception.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool isBusy)
    {
        _friendlyNameTextBox.Enabled = !isBusy;
        _profileComboBox.Enabled = !isBusy;
        _usernameTextBox.Enabled = !isBusy;
        _passwordTextBox.Enabled = !isBusy;
        _connectButton.Enabled = !isBusy;
        _saveProfileButton.Enabled = !isBusy;
        _deleteProfileButton.Enabled = !isBusy;
        _browseButton.Enabled = !isBusy;
        _deployButton.Enabled = !isBusy && _videoTecClient != null && !string.IsNullOrWhiteSpace(_selectedProjectFile);
        _progressBar.Visible = isBusy;
        _progressBar.MarqueeAnimationSpeed = isBusy ? 30 : 0;
        UseWaitCursor = isBusy;
    }

    private void UpdateDeployButtonState()
    {
        _deployButton.Enabled = _videoTecClient != null && !string.IsNullOrWhiteSpace(_selectedProjectFile);
    }

    private void SetStatus(string message)
    {
        _statusTextBox.Text = message + Environment.NewLine;
    }

    private void AppendSuccess(string message)
    {
        AppendStatus($"? {message}");
    }

    private void AppendWarning(string message)
    {
        AppendStatus($"? {message}");
    }

    private void AppendError(string message)
    {
        AppendStatus($"? {message}");
    }

    private void AppendStatus(string message)
    {
        if (_statusTextBox.Text == "Ready.")
        {
            _statusTextBox.Clear();
        }

        _statusTextBox.AppendText($"{DateTime.Now:T}  {message}{Environment.NewLine}");
    }

    private Panel CreateSectionPanel(string title)
    {
        var outer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BorderColor,
            Padding = new Padding(1),
            Margin = new Padding(0, 0, 14, 0)
        };

        var inner = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = PanelBackground
        };

        inner.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        inner.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            Padding = new Padding(14, 0, 14, 0),
            ForeColor = PrimaryText,
            Font = new Font(Font.FontFamily, 11, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = PanelBackground
        };

        var contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelBackground
        };

        inner.Controls.Add(header, 0, 0);
        inner.Controls.Add(contentHost, 0, 1);
        outer.Controls.Add(inner);

        return contentHost;
    }

    private static Panel CreateLeftAlignedButtonPanel(Control button)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelBackground
        };

        button.Left = 0;
        button.Top = 8;
        panel.Controls.Add(button);

        return panel;
    }

    private static Panel CreateCenteredButtonPanel(Control button)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelBackground
        };

        panel.Resize += (_, _) =>
        {
            button.Left = (panel.ClientSize.Width - button.Width) / 2;
            button.Top = (panel.ClientSize.Height - button.Height) / 2;
        };

        panel.Controls.Add(button);
        return panel;
    }

    private static void ConfigureInput(TextBox textBox)
    {
        textBox.Dock = DockStyle.Fill;
        textBox.BackColor = InputBackground;
        textBox.ForeColor = PrimaryText;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Margin = new Padding(0, 4, 0, 4);
    }

    private static void ConfigureComboBox(ComboBox comboBox)
    {
        comboBox.Dock = DockStyle.Fill;
        comboBox.BackColor = InputBackground;
        comboBox.ForeColor = PrimaryText;
        comboBox.FlatStyle = FlatStyle.Flat;
        comboBox.Margin = new Padding(0, 4, 0, 4);
    }

    private static void ConfigureButton(Button button, string text, bool isPrimary, bool deEmphasized = false)
    {
        button.Text = text;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = isPrimary ? AccentColor : BorderColor;
        button.FlatAppearance.MouseOverBackColor = isPrimary ? AccentColor : Color.FromArgb(0x45, 0x45, 0x45);
        button.BackColor = isPrimary ? AccentColor : ButtonBackground;
        button.ForeColor = deEmphasized ? SecondaryText : PrimaryText;
        button.Margin = new Padding(0, 4, 10, 4);
    }

    private static void ConfigureValueLabel(Label label, string text)
    {
        label.Text = text;
        label.ForeColor = SecondaryText;
        label.AutoEllipsis = true;
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
    }

    private static void AddLabel(TableLayoutPanel layout, string text, int column, int row)
    {
        layout.Controls.Add(new Label
        {
            Text = text,
            AutoSize = false,
            Dock = DockStyle.Fill,
            ForeColor = SecondaryText,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 4, 8, 4)
        }, column, row);
    }
}
