using CTF.Common.Models;
using Microsoft.VisualBasic.ApplicationServices;

namespace CTF.Client
{
    public partial class MainForm : Form
    {
        private readonly ServerConnection _server;
        private Panel panelContent;
        private Label lblScore;
        private Button? btnActive;

        public MainForm(ServerConnection server)
        {
            _server = server;
            SetupUI();
            SetupBroadcastListener();

        }

        private void SetupUI()
        {
            Text = $"CTF Platform — {_server.CurrentUser?.Username}";
            Size = new Size(1100, 700);
            MinimumSize = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(18, 18, 28);

            // Important: panelContent first, then sidebar
            // This way Dock works correctly
            panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 18, 28)
            };
            Controls.Add(panelContent);

            Panel sidebar = BuildSidebar();
            Controls.Add(sidebar);

            // Open Challenges screen by default
            OpenPanel(new ChallengesForm(_server));
        }

        private Panel BuildSidebar()
        {
            Panel sidebar = new()
            {
                Dock = DockStyle.Left,
                Width = 180,
                BackColor = Color.FromArgb(20, 20, 32)
            };

            // Logo
            sidebar.Controls.Add(new Label
            {
                Text = "🚩 CTF Platform",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 100),
                Location = new Point(12, 16),
                Size = new Size(156, 22)
            });

            // Divider
            sidebar.Controls.Add(new Panel
            {
                Location = new Point(12, 44),
                Size = new Size(156, 1),
                BackColor = Color.FromArgb(45, 45, 65)
            });

            // User info
            sidebar.Controls.Add(new Label
            {
                Text = _server.CurrentUser?.Username ?? "",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(12, 52),
                Size = new Size(156, 20)
            });

            sidebar.Controls.Add(new Label
            {
                Text = _server.CurrentUser?.Role.ToString() ?? "",
                Font = new Font("Segoe UI", 8),
                ForeColor = RoleColor(_server.CurrentUser?.Role ?? UserRole.Player),
                Location = new Point(12, 72),
                Size = new Size(156, 16)
            });

            lblScore = new Label
            {
                Text = $"⭐ {_server.CurrentUser?.Score ?? 0} pts",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gold,
                Location = new Point(12, 88),
                Size = new Size(156, 16)
            };
            sidebar.Controls.Add(lblScore);

            // Divider
            sidebar.Controls.Add(new Panel
            {
                Location = new Point(12, 112),
                Size = new Size(156, 1),
                BackColor = Color.FromArgb(45, 45, 65)
            });

            // Nav buttons
            int y = 122;
            UserRole role = _server.CurrentUser?.Role ?? UserRole.Player;

            AddNavButton(sidebar, "🏆  Challenges", ref y,
                () => OpenPanel(new ChallengesForm(_server)));

            AddNavButton(sidebar, "📊  Scoreboard", ref y,
                () => OpenPanel(new ScoreboardForm(_server)));

            if (role == UserRole.Creator || role == UserRole.Admin)
                AddNavButton(sidebar, "➕  Add Challenge", ref y,
                    () => OpenPanel(new CreateChallengeForm(_server)));

            if (role == UserRole.Admin)
                AddNavButton(sidebar, "👥  Manage Users", ref y,
                    () => OpenPanel(new ManageUsersForm(_server)));

            // Logout בתחתית
            Button btnLogout = new()
            {
                Text = "🚪  Logout",
                Dock = DockStyle.Bottom,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 20, 20),
                ForeColor = Color.OrangeRed,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0)
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += (s, e) =>
            {
                _server.Disconnect();
                new LoginForm(new ServerConnection()).Show();
                this.Hide();
            };
            sidebar.Controls.Add(btnLogout);

            return sidebar;
        }

        private void AddNavButton(Panel sidebar, string text, ref int y, Action onClick)
        {
            Button btn = new()
            {
                Text = text,
                Location = new Point(0, y),
                Size = new Size(180, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(170, 170, 195),
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 30, 50);
            btn.Click += (s, e) =>
            {
                SetActiveButton(btn);
                onClick();
            };
            sidebar.Controls.Add(btn);
            y += 42;
        }

        private void SetActiveButton(Button btn)
        {
            if (btnActive != null)
            {
                btnActive.BackColor = Color.Transparent;
                btnActive.ForeColor = Color.FromArgb(170, 170, 195);
            }
            btn.BackColor = Color.FromArgb(30, 50, 30);
            btn.ForeColor = Color.FromArgb(100, 220, 100);
            btnActive = btn;
        }

        private void OpenPanel(UserControl control)
        {
            panelContent.Controls.Clear();
            control.Dock = DockStyle.Fill;
            panelContent.Controls.Add(control);
        }

        private void SetupBroadcastListener()
        {
            _server.OnBroadcastReceived += (message) =>
            {
                if (InvokeRequired) { Invoke(() => HandleBroadcast(message)); return; }
                HandleBroadcast(message);
            };
        }

        private void HandleBroadcast(string message)
        {
            if (message.StartsWith("SCORE_UPDATE:"))
            {
                string[] parts = message.Split(':');
                if (parts.Length == 3 && parts[1] == _server.CurrentUser?.Username)
                    lblScore.Text = $"⭐ {parts[2]} pts";

                if (panelContent.Controls.Count > 0 &&
                    panelContent.Controls[0] is ScoreboardForm sb)
                    _ = sb.LoadScoreboardAsync();
            }
            else if (message.StartsWith("NEW_CHALLENGE:"))
            {
                string title = message["NEW_CHALLENGE:".Length..];
                MessageBox.Show($"🚩 New challenge: {title}", "New Challenge!",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static Color RoleColor(UserRole role) => role switch
        {
            UserRole.Admin => Color.OrangeRed,
            UserRole.Creator => Color.CornflowerBlue,
            _ => Color.MediumSeaGreen
        };
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            _server.Disconnect();
            Application.Exit();
        }
    }
}

