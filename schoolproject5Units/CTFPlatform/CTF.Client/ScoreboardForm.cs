using CTF.Common.Models;

namespace CTF.Client
{
    public partial class ScoreboardForm : UserControl
    {
        private readonly ServerConnection _server;
        private DataGridView dgv;
        private Label lblLastUpdate;

        public ScoreboardForm(ServerConnection server)
        {
            _server = server;
            SetupUI();
            _ = LoadScoreboardAsync();
        }

        private void SetupUI()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(18, 18, 28);

            Label lblHead = new()
            {
                Text = "📊 Scoreboard",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 100),
                Location = new Point(30, 20),
                Size = new Size(400, 35)
            };
            Controls.Add(lblHead);

            Button btnRefresh = new()
            {
                Text = "🔄 Refresh",
                Location = new Point(700, 22),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(40, 80, 40),
                ForeColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRefresh.Click += async (s, e) => await LoadScoreboardAsync();
            Controls.Add(btnRefresh);

            lblLastUpdate = new Label
            {
                Location = new Point(30, 58),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9),
                Text = "Loading..."
            };
            Controls.Add(lblLastUpdate);

            dgv = new DataGridView
            {
                Location = new Point(30, 85),
                Size = new Size(790, 500),
                BackgroundColor = Color.FromArgb(22, 22, 34),
                GridColor = Color.FromArgb(40, 40, 55),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 11),
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 42 },
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Header style
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(30, 30, 45),
                ForeColor = Color.FromArgb(150, 150, 180),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                SelectionBackColor = Color.FromArgb(30, 30, 45)
            };

            // Row style
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(22, 22, 34),
                ForeColor = Color.White,
                SelectionBackColor = Color.FromArgb(35, 55, 35),
                SelectionForeColor = Color.White,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                Padding = new Padding(5)
            };

            // Alternating rows
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(26, 26, 40),
                ForeColor = Color.White,
                SelectionBackColor = Color.FromArgb(35, 55, 35),
                SelectionForeColor = Color.White
            };

            dgv.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Rank", HeaderText = "#", FillWeight = 8 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Username", HeaderText = "Player", FillWeight = 35 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Score", HeaderText = "Score", FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Solved", HeaderText = "Solved", FillWeight = 20 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "LastSolve", HeaderText = "Last Solve", FillWeight = 30 });

            dgv.CellFormatting += Dgv_CellFormatting;
            Controls.Add(dgv);
        }

        public async Task LoadScoreboardAsync()
        {
            List<ScoreboardEntry> entries = await _server.GetScoreboardAsync();

            if (InvokeRequired) { Invoke(() => PopulateGrid(entries)); return; }
            PopulateGrid(entries);
        }

        private void PopulateGrid(List<ScoreboardEntry> entries)
        {
            dgv.Rows.Clear();
            foreach (ScoreboardEntry e in entries)
            {
                string rank = e.Rank switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => e.Rank.ToString() };
                string last = e.LastSolve == DateTime.MinValue ? "—" : e.LastSolve.ToString("HH:mm dd/MM");
                dgv.Rows.Add(rank, e.Username, e.Score, e.SolvedCount, last);
            }
            lblLastUpdate.Text = $"Last updated: {DateTime.Now:HH:mm:ss}";
        }

        // Highlight 
        private void Dgv_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string? username = dgv.Rows[e.RowIndex].Cells["Username"].Value?.ToString();
            if (username == _server.CurrentUser?.Username)
            {
                dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(20, 50, 20);
                dgv.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.LightGreen;
                dgv.Rows[e.RowIndex].DefaultCellStyle.Font =
                    new Font("Segoe UI", 11, FontStyle.Bold);
            }
        }
    }
}
