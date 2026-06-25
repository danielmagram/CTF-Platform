using CTF.Common.Models;

namespace CTF.Client
{
    public partial class ManageUsersForm : UserControl
    {
        private readonly ServerConnection _server;
        private List<User> _users = new();
        private DataGridView dgv;
        private Label lblStatus;

        public ManageUsersForm(ServerConnection server)
        {
            _server = server;
            SetupUI();
            _ = LoadUsersAsync();
        }

        private void SetupUI()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(18, 18, 28);

            // 1. First Fill — DataGridView
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.FromArgb(22, 22, 34),
                GridColor = Color.FromArgb(35, 35, 50),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Segoe UI", 10),
                ColumnHeadersHeight = 38,
                RowTemplate = { Height = 38 },
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(28, 28, 44),
                ForeColor = Color.FromArgb(140, 140, 170),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                SelectionBackColor = Color.FromArgb(28, 28, 44),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };

            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(22, 22, 34),
                ForeColor = Color.White,
                SelectionBackColor = Color.FromArgb(35, 45, 65),
                SelectionForeColor = Color.White,
                Padding = new Padding(8, 0, 0, 0)
            };

            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(25, 25, 38),
                ForeColor = Color.White,
                SelectionBackColor = Color.FromArgb(35, 45, 65),
                SelectionForeColor = Color.White
            };

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", FillWeight = 6 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Username", HeaderText = "Username", FillWeight = 28 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Role", HeaderText = "Role", FillWeight = 18 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Score", HeaderText = "Score", FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Registered", HeaderText = "Registered", FillWeight = 22 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Active", HeaderText = "Active", FillWeight = 10 });

            dgv.CellFormatting += Dgv_CellFormatting;
            Controls.Add(dgv);

            // 2. Then Top in reverse order (last = topmost)

            // Status label
            lblStatus = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                Text = "",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };
            Controls.Add(lblStatus);

            // Buttons panel
            Panel btnPanel = new()
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent,
                Padding = new Padding(20, 6, 0, 0)
            };

            Button btnRefresh = MakeButton("🔄 Refresh", Color.FromArgb(40, 60, 40), Color.LightGreen, 0);
            btnRefresh.Click += async (s, e) => await LoadUsersAsync();
            btnPanel.Controls.Add(btnRefresh);

            Button btnMakeCreator = MakeButton("⬆️ Make Creator", Color.FromArgb(30, 50, 100), Color.CornflowerBlue, 110);
            btnMakeCreator.Click += async (s, e) => await ChangeRole("Creator");
            btnPanel.Controls.Add(btnMakeCreator);

            Button btnMakePlayer = MakeButton("⬇️ Make Player", Color.FromArgb(40, 40, 20), Color.Goldenrod, 260);
            btnMakePlayer.Click += async (s, e) => await ChangeRole("Player");
            btnPanel.Controls.Add(btnMakePlayer);

            Controls.Add(btnPanel);

            // Header — last = topmost
            Controls.Add(new Label
            {
                Text = "👥 Manage Users",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.OrangeRed,
                Dock = DockStyle.Top,
                Height = 50,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            });
        }

        private async Task LoadUsersAsync()
        {
            _users = await _server.GetAllUsersAsync();
            if (InvokeRequired) { Invoke(() => PopulateGrid(_users)); return; }
            PopulateGrid(_users);
        }

        private void PopulateGrid(List<User> users)
        {
            dgv.Rows.Clear();
            foreach (User u in users)
                dgv.Rows.Add(
                    u.Id,
                    u.Username,
                    u.Role.ToString(),
                    u.Score,
                    u.RegisteredAt.ToString("dd/MM/yyyy"),
                    u.IsActive ? "✅" : "❌");

            lblStatus.Text = $"{users.Count} users total";
            lblStatus.ForeColor = Color.Gray;
        }

        private async Task ChangeRole(string newRole)
        {
            if (dgv.SelectedRows.Count == 0)
            {
                ShowStatus("Please select a user first", Color.OrangeRed);
                return;
            }

            int userId = Convert.ToInt32(dgv.SelectedRows[0].Cells["Id"].Value);
            string username = dgv.SelectedRows[0].Cells["Username"].Value?.ToString() ?? "";
            string currentRole = dgv.SelectedRows[0].Cells["Role"].Value?.ToString() ?? "";

            if (currentRole == "Admin")
            {
                ShowStatus("Cannot change Admin role", Color.OrangeRed);
                return;
            }

            if (currentRole == newRole)
            {
                ShowStatus($"{username} is already {newRole}", Color.Gray);
                return;
            }

            var confirm = MessageBox.Show(
                $"Change '{username}' from {currentRole} to {newRole}?",
                "Confirm Role Change",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            var (success, message) = await _server.ChangeUserRoleAsync(userId, newRole);
            ShowStatus(success ? $"✅ {username} is now {newRole}" : $"❌ {message}",
                success ? Color.LightGreen : Color.OrangeRed);

            if (success) await LoadUsersAsync();
        }

        private void ShowStatus(string text, Color color)
        {
            lblStatus.Text = text;
            lblStatus.ForeColor = color;
        }

        private void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgv.Columns[e.ColumnIndex].Name != "Role") return;

            string? role = e.Value?.ToString();
            e.CellStyle.ForeColor = role switch
            {
                "Admin" => Color.OrangeRed,
                "Creator" => Color.CornflowerBlue,
                _ => Color.MediumSeaGreen
            };
        }

        private static Button MakeButton(string text, Color bg, Color fg, int x)
        {
            Button btn = new()
            {
                Text = text,
                Location = new Point(x, 0),
                Size = new Size(140, 32),
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }
    }
}