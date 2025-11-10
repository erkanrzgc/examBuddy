using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace ExamBuddy
{
    public sealed class MainForm : Form
    {
        private readonly List<TaskItem> _tasks = new();
        private int _nextId = 1;

        private readonly ListView _taskListView = new();
        private readonly TextBox _titleTextBox = new();
        private readonly TextBox _descriptionTextBox = new();
        private readonly DateTimePicker _duePicker = new();
        private readonly CheckBox _dueToggle = new();
        private readonly ComboBox _statusCombo = new();
        private readonly Button _addButton = new();
        private readonly Button _updateButton = new();
        private readonly Button _deleteButton = new();
        private readonly Button _completeButton = new();
        private readonly Button _todayButton = new();
        private readonly Label _infoLabel = new();
        private NotifyIcon? _notifyIcon;
        private System.Windows.Forms.Timer? _reminderTimer;
        private readonly HashSet<int> _notifiedTaskIds = new();
        private readonly TimeSpan _reminderLeadTime = TimeSpan.FromMinutes(1);
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm";

        private string DataFilePath =>
            Path.Combine(AppContext.BaseDirectory, "tasks.db");

        public MainForm()
        {
            InitializeTheme();
            InitializeComponent();
            LoadTasks();
            RefreshTaskList();
            InitializeReminders();
        }

        private void InitializeTheme()
        {
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.FromArgb(220, 220, 220);
            Padding = new Padding(16);

            try
            {
                Font = new Font("JetBrains Mono", 9f, FontStyle.Regular, GraphicsUnit.Point);
            }
            catch (ArgumentException)
            {
                Font = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Regular);
            }

            Text = "Exam Buddy";
            MinimumSize = new Size(900, 600);
            Size = new Size(960, 600);
            StartPosition = FormStartPosition.CenterScreen;
        }

        private void InitializeComponent()
        {
            var header = new Label
            {
                Text = "Exam Buddy",
                Dock = DockStyle.Top,
                Font = new Font(Font, FontStyle.Bold),
                Height = 36,
                ForeColor = Color.FromArgb(255, 203, 107),
                TextAlign = ContentAlignment.MiddleLeft
            };

            var topPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                Padding = new Padding(0, 12, 0, 12)
            };
            for (int i = 0; i < 8; i++)
            {
                topPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            var titleLabel = CreateMutedLabel("Ders");
            _titleTextBox.Dock = DockStyle.Top;
            _titleTextBox.Height = 28;
            _titleTextBox.BackColor = Color.FromArgb(45, 45, 45);
            _titleTextBox.ForeColor = ForeColor;
            _titleTextBox.BorderStyle = BorderStyle.FixedSingle;
            _titleTextBox.Margin = new Padding(0, 4, 0, 12);
            _titleTextBox.Padding = new Padding(4);

            var duePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 42,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            _dueToggle.Text = "Çalışma saati kullan";
            _dueToggle.AutoSize = true;
            _dueToggle.ForeColor = Color.FromArgb(152, 195, 121);
            _dueToggle.Checked = false;
            _dueToggle.Margin = new Padding(0, 6, 16, 0);
            _dueToggle.CheckedChanged += (_, _) =>
            {
                _duePicker.Enabled = _dueToggle.Checked;
            };

            _duePicker.Format = DateTimePickerFormat.Custom;
            _duePicker.CustomFormat = "yyyy-MM-dd HH:mm";
            _duePicker.ShowUpDown = true;
            _duePicker.Enabled = false;
            _duePicker.Width = 240;
            _duePicker.Margin = new Padding(0, 0, 0, 0);
            _duePicker.CalendarForeColor = Color.Black;

            duePanel.Controls.Add(_dueToggle);
            duePanel.Controls.Add(_duePicker);

            var statusLabel = CreateMutedLabel("Durum");
            _statusCombo.Dock = DockStyle.Top;
            _statusCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _statusCombo.BackColor = Color.FromArgb(45, 45, 45);
            _statusCombo.ForeColor = ForeColor;
            _statusCombo.DataSource = Enum.GetValues(typeof(TaskStatus));

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0),
                Margin = new Padding(0, 4, 0, 0)
            };

            ConfigureButton(_addButton, "Ekle", Color.FromArgb(122, 162, 247), OnAddTask);
            ConfigureButton(_updateButton, "Güncelle", Color.FromArgb(198, 120, 221), OnUpdateTask);
            ConfigureButton(_deleteButton, "Sil", Color.FromArgb(224, 108, 117), OnDeleteTask);
            ConfigureButton(_completeButton, "Tamamlandı", Color.FromArgb(152, 195, 121), OnCompleteTask);
            ConfigureButton(_todayButton, "Bugün", Color.FromArgb(255, 203, 107), OnShowToday);

            buttonPanel.Controls.AddRange(new Control[]
            {
                _addButton, _updateButton, _deleteButton, _completeButton, _todayButton
            });

            var descriptionLabel = CreateMutedLabel("Detay");
            _descriptionTextBox.Dock = DockStyle.Fill;
            _descriptionTextBox.Multiline = true;
            _descriptionTextBox.Height = 90;
            _descriptionTextBox.BackColor = Color.FromArgb(45, 45, 45);
            _descriptionTextBox.ForeColor = ForeColor;
            _descriptionTextBox.BorderStyle = BorderStyle.FixedSingle;
            _descriptionTextBox.ScrollBars = ScrollBars.Vertical;
            _descriptionTextBox.Margin = new Padding(0);

            topPanel.Controls.Add(titleLabel, 0, 0);
            topPanel.Controls.Add(_titleTextBox, 0, 1);
            topPanel.Controls.Add(CreateMutedLabel("Çalışma Saati"), 0, 2);
            topPanel.Controls.Add(duePanel, 0, 3);
            topPanel.Controls.Add(statusLabel, 0, 4);

            var statusAndButtonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0),
                Margin = new Padding(0, 4, 0, 0)
            };

            _statusCombo.Margin = new Padding(0, 0, 0, 8);
            statusAndButtonsPanel.Controls.Add(_statusCombo);
            statusAndButtonsPanel.Controls.Add(buttonPanel);

            topPanel.Controls.Add(statusAndButtonsPanel, 0, 5);
            topPanel.Controls.Add(descriptionLabel, 0, 6);
            topPanel.Controls.Add(_descriptionTextBox, 0, 7);

            _taskListView.Dock = DockStyle.Fill;
            _taskListView.FullRowSelect = true;
            _taskListView.GridLines = true;
            _taskListView.HideSelection = false;
            _taskListView.MultiSelect = false;
            _taskListView.View = View.Details;
            _taskListView.BackColor = Color.FromArgb(37, 37, 38);
            _taskListView.ForeColor = ForeColor;
            _taskListView.Columns.Add("Sıra", 60);
            _taskListView.Columns.Add("Ders");
            _taskListView.Columns.Add("Durum");
            _taskListView.Columns.Add("Detay");
            _taskListView.Columns.Add("Çalışma Saati");
            _taskListView.SelectedIndexChanged += (_, _) => SyncSelectedTask();

            _infoLabel.Dock = DockStyle.Bottom;
            _infoLabel.Height = 30;
            _infoLabel.TextAlign = ContentAlignment.MiddleLeft;
            _infoLabel.ForeColor = Color.FromArgb(120, 120, 120);

            Controls.Add(_taskListView);
            Controls.Add(_infoLabel);
            Controls.Add(topPanel);
            Controls.Add(header);
        }

        private static Label CreateMutedLabel(string text) =>
            new()
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 20,
                ForeColor = Color.FromArgb(120, 120, 120)
            };

        private static void ConfigureButton(Button button, string text, Color accent, EventHandler onClick)
        {
            button.Text = text;
            button.AutoSize = true;
            button.Margin = new Padding(0, 8, 12, 0);
            button.Padding = new Padding(12, 6, 12, 6);
            button.BackColor = accent;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.ForeColor = Color.Black;
            button.Click += onClick;
        }

        private void LoadTasks()
        {
            if (!File.Exists(DataFilePath))
            {
                return;
            }

            var content = File.ReadAllText(DataFilePath);
            if (string.IsNullOrWhiteSpace(content))
            {
                return;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            try
            {
                var database = JsonSerializer.Deserialize<TaskDatabase>(content, options);
                if (database is not null)
                {
                    _tasks.AddRange(database.Tasks);
                    _nextId = Math.Max(database.NextId, _tasks.Count == 0 ? 1 : _tasks.Max(t => t.Id) + 1);
                    return;
                }
            }
            catch (JsonException)
            {
                LoadLegacyFormat(content);
                return;
            }

            LoadLegacyFormat(content);
        }

        private void LoadLegacyFormat(string content)
        {
            using var reader = new StringReader(content);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = line.Split('|');
                if (parts.Length < 4 || !int.TryParse(parts[0], out var id))
                {
                    continue;
                }

                var task = new TaskItem
                {
                    Id = id,
                    Title = parts.ElementAtOrDefault(3) ?? string.Empty,
                    DueDate = parts.ElementAtOrDefault(2) == "-" ? string.Empty : parts.ElementAtOrDefault(2) ?? string.Empty,
                    Status = TaskStatusExtensions.FromToken(parts.ElementAtOrDefault(1) ?? string.Empty),
                    Description = parts.Length >= 5 ? parts[4] : string.Empty
                };
                _tasks.Add(task);
                _nextId = Math.Max(_nextId, id + 1);
            }
        }

        private void SaveTasks()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DataFilePath)!);
            var database = new TaskDatabase
            {
                NextId = _nextId,
                Tasks = _tasks.OrderBy(t => t.Id).ToList()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var json = JsonSerializer.Serialize(database, options);
            File.WriteAllText(DataFilePath, json);
        }

        private void RefreshTaskList()
        {
            _taskListView.BeginUpdate();
            _taskListView.Items.Clear();

            var sortedTasks = _tasks
                .OrderByDescending(t => t.Status == TaskStatus.Completed)
                .ThenBy(t => ParseDueDate(t.DueDate))
                .ThenBy(t => t.Title, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            RemoveExpiredTasks(sortedTasks);

            for (var index = 0; index < sortedTasks.Count; index++)
            {
                var task = sortedTasks[index];
                task.Id = index + 1;
                var item = new ListViewItem(task.ToRow()) { Tag = task };
                _taskListView.Items.Add(item);
            }

            AutoSizeColumns();
            _taskListView.EndUpdate();

            _infoLabel.Text = _tasks.Count == 0
                ? "Henüz görev yok. Başlık girip 'Ekle' tuşuna bas."
                : $"{_tasks.Count} görev listelendi";
        }

        private static DateTime ParseDueDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "-")
            {
                return DateTime.MaxValue;
            }

            if (DateTime.TryParseExact(
                    value,
                    new[] { DateTimeFormat, "yyyy-MM-dd" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsed))
            {
                return parsed;
            }

            return DateTime.MaxValue;
        }

        private void RemoveExpiredTasks(List<TaskItem> tasks)
        {
            var now = DateTime.Now;
            bool removed = false;

            for (int i = tasks.Count - 1; i >= 0; i--)
            {
                var task = tasks[i];
                if (string.IsNullOrWhiteSpace(task.DueDate))
                {
                    continue;
                }

                var due = ParseDueDate(task.DueDate);
                if (due == DateTime.MaxValue)
                {
                    continue;
                }

                if (now > due.AddHours(2))
                {
                    tasks.RemoveAt(i);
                    _tasks.Remove(task);
                    removed = true;
                }
            }

            if (removed)
            {
                SaveTasks();
            }
        }

        private void AutoSizeColumns()
        {
            if (_taskListView.Columns.Count < 5)
            {
                return;
            }

            const int idWidth = 80;
            var totalWidth = _taskListView.ClientSize.Width;
            var dynamicWidth = Math.Max(totalWidth - idWidth, 400);
            var each = dynamicWidth / 4;

            _taskListView.Columns[0].Width = idWidth;
            for (int i = 1; i < _taskListView.Columns.Count; i++)
            {
                _taskListView.Columns[i].Width = each;
            }

            _taskListView.Resize -= OnListViewResize;
            _taskListView.Resize += OnListViewResize;
        }

        private void OnListViewResize(object? sender, EventArgs e)
        {
            AutoSizeColumns();
        }

        private TaskItem? SelectedTask =>
            _taskListView.SelectedItems.Count > 0
                ? _taskListView.SelectedItems[0].Tag as TaskItem
                : null;

        private void SyncSelectedTask()
        {
            var task = SelectedTask;
            if (task == null)
            {
                _titleTextBox.Clear();
                _dueToggle.Checked = false;
                _statusCombo.SelectedItem = TaskStatus.Pending;
                return;
            }

            _titleTextBox.Text = task.Title;
            if (string.IsNullOrWhiteSpace(task.DueDate))
            {
                _dueToggle.Checked = false;
            }
            else if (DateTime.TryParseExact(task.DueDate,
                     new[] { "yyyy-MM-dd HH:mm", "yyyy-MM-dd" },
                     CultureInfo.InvariantCulture,
                     DateTimeStyles.None,
                     out var parsed))
            {
                _dueToggle.Checked = true;
                _duePicker.Value = parsed;
            }
            else
            {
                _dueToggle.Checked = false;
            }

            _statusCombo.SelectedItem = task.Status;
            _descriptionTextBox.Text = task.Description;
        }

        private void OnAddTask(object? sender, EventArgs e)
        {
            var title = _titleTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show(this, "Ders adı boş olamaz.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var task = new TaskItem
            {
                Id = _nextId++,
                Title = title,
                Status = (TaskStatus)_statusCombo.SelectedItem!,
                Description = _descriptionTextBox.Text.Trim()
            };

            if (_dueToggle.Checked)
            {
                task.DueDate = _duePicker.Value.ToString(DateTimeFormat);
            }

            _tasks.Add(task);
            SaveTasks();
            RefreshTaskList();
            _taskListView.SelectedItems.Clear();
            _titleTextBox.Clear();
            _dueToggle.Checked = false;
            _statusCombo.SelectedItem = TaskStatus.Pending;
            _descriptionTextBox.Clear();
            CheckReminders();
        }

        private void OnUpdateTask(object? sender, EventArgs e)
        {
            var task = SelectedTask;
            if (task == null)
            {
                MessageBox.Show(this, "Lütfen güncellenecek görevi seç.", "Bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var title = _titleTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show(this, "Ders adı boş olamaz.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            task.Title = title;
            task.Status = (TaskStatus)_statusCombo.SelectedItem!;
            task.DueDate = _dueToggle.Checked
                ? _duePicker.Value.ToString(DateTimeFormat)
                : string.Empty;
            task.Description = _descriptionTextBox.Text.Trim();

            _notifiedTaskIds.Remove(task.Id);

            SaveTasks();
            RefreshTaskList();
            CheckReminders();
        }

        private void OnDeleteTask(object? sender, EventArgs e)
        {
            var task = SelectedTask;
            if (task == null)
            {
                MessageBox.Show(this, "Silmek için görev seç.", "Bilgi",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(this,
                $"“{task.Title}” görevini silmek istediğine emin misin?",
                "Sil",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            _tasks.Remove(task);
            _notifiedTaskIds.Remove(task.Id);
            SaveTasks();
            RefreshTaskList();
        }

        private void OnCompleteTask(object? sender, EventArgs e)
        {
            var task = SelectedTask;
            if (task == null)
            {
                MessageBox.Show(this, "Tamamlandı olarak işaretlemek için görev seç.",
                    "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            task.Status = TaskStatus.Completed;
            _notifiedTaskIds.Remove(task.Id);
            SaveTasks();
            RefreshTaskList();
        }

        private void OnShowToday(object? sender, EventArgs e)
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var todays = _tasks
                .Where(t => !string.IsNullOrWhiteSpace(t.DueDate) && t.DueDate == today)
                .OrderBy(t => t.Id)
                .ToList();

            if (todays.Count == 0)
            {
                MessageBox.Show(this, "Bugün için atanmış görev yok.", "Bugün",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var message = string.Join(Environment.NewLine,
                todays.Select(t =>
                {
                    var details = string.IsNullOrWhiteSpace(t.Description)
                        ? string.Empty
                        : $" - {t.Description}";
                    return $"• {t.Title} [{t.Status.ToDisplay()}]{details}";
                }));

            MessageBox.Show(this, message, $"Bugün ({today})",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void InitializeReminders()
        {
            var trayIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            _notifyIcon = new NotifyIcon
            {
                Icon = trayIcon ?? SystemIcons.Application,
                Visible = true,
                Text = "Exam Buddy"
            };

            _reminderTimer = new System.Windows.Forms.Timer
            {
                Interval = (int)TimeSpan.FromMinutes(1).TotalMilliseconds
            };
            _reminderTimer.Tick += (_, _) => CheckReminders();
            _reminderTimer.Start();
        }

        private void CheckReminders()
        {
            if (_notifyIcon is null)
            {
                return;
            }

            var now = DateTime.Now;
            foreach (var task in _tasks)
            {
                if (task.Status == TaskStatus.Completed ||
                    string.IsNullOrWhiteSpace(task.DueDate) ||
                    _notifiedTaskIds.Contains(task.Id))
                {
                    continue;
                }

                if (!DateTime.TryParseExact(task.DueDate,
                        new[] { "yyyy-MM-dd HH:mm", "yyyy-MM-dd" },
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var dueDate))
                {
                    continue;
                }

                if (now >= dueDate - _reminderLeadTime)
                {
                    var detail = string.IsNullOrWhiteSpace(task.Description)
                        ? string.Empty
                        : Environment.NewLine + task.Description;

                    _notifyIcon.BalloonTipTitle = "Görev hatırlatıcısı";
                    _notifyIcon.BalloonTipText =
                        $"{task.Title}\nÇalışma saati: {dueDate:yyyy-MM-dd HH:mm}{detail}";
                    _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                    _notifyIcon.ShowBalloonTip(5000);
                    _notifiedTaskIds.Add(task.Id);
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);

            if (_reminderTimer is not null)
            {
                _reminderTimer.Stop();
                _reminderTimer.Dispose();
            }

            if (_notifyIcon is not null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }

        }

        private sealed class TaskDatabase
        {
            public int NextId { get; set; } = 1;

            public List<TaskItem> Tasks { get; set; } = new();
        }
    }
}


