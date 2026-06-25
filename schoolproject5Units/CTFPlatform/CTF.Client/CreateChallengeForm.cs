using CTF.Common.Packets;

namespace CTF.Client
{
    public partial class CreateChallengeForm : UserControl
    {
        private readonly ServerConnection _server;

        private TextBox txtTitle, txtFlag;
        private RichTextBox rtbDescription;
        private ComboBox cmbCategory, cmbDifficulty;
        private NumericUpDown numPoints;
        private Button btnCreate;
        private Label lblStatus;

        // Hints
        private ListBox lstPendingHints;
        private TextBox txtHintContent;
        private NumericUpDown numHintCost;
        private List<(string content, int cost)> _pendingHints = new();

        // Files
        private ListBox lstPendingFiles;
        private List<(string fileName, byte[] data)> _pendingFiles = new();

        private const long MaxFileSize = 100 * 1024 * 1024;
        public CreateChallengeForm(ServerConnection server)
        {
            _server = server;
            SetupUI();
        }

        private void SetupUI()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(18, 18, 28);
            AutoScroll = true;

            Controls.Add(new Label
            {
                Text = "➕ Create Challenge",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 180, 255),
                Location = new Point(40, 20),
                Size = new Size(400, 35)
            });

            int y = 75;

            txtTitle = AddField("Challenge Title", 40, ref y);
            rtbDescription = AddRichField("Description", 40, ref y);
            txtFlag = AddField("Flag (e.g. CTF{secret_flag})", 40, ref y);

            // Category + Difficulty + Points
            AddLabel("Category", 40, y);
            cmbCategory = new ComboBox
            {
                Location = new Point(40, y + 22),
                Size = new Size(180, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(35, 35, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbCategory.Items.AddRange(new[] { "Crypto", "Web", "Forensics", "Reversing", "Pwn", "Misc", "Other" });
            cmbCategory.SelectedIndex = 0;
            Controls.Add(cmbCategory);

            AddLabel("Difficulty", 240, y);
            cmbDifficulty = new ComboBox
            {
                Location = new Point(240, y + 22),
                Size = new Size(150, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(35, 35, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbDifficulty.Items.AddRange(new[] { "Easy", "Medium", "Hard", "Insane" });
            cmbDifficulty.SelectedIndex = 0;
            Controls.Add(cmbDifficulty);

            AddLabel("Points", 410, y);
            numPoints = new NumericUpDown
            {
                Location = new Point(410, y + 22),
                Size = new Size(100, 28),
                Minimum = 10,
                Maximum = 1000,
                Value = 100,
                Increment = 10,
                BackColor = Color.FromArgb(35, 35, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11)
            };
            Controls.Add(numPoints);
            y += 65;

            // ===== HINTS SECTION =====
            Controls.Add(new Label
            {
                Text = "💡 Hints (optional)",
                Location = new Point(40, y),
                AutoSize = true,
                ForeColor = Color.Yellow,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            });
            y += 28;

            txtHintContent = new TextBox
            {
                Location = new Point(40, y),
                Size = new Size(320, 26),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(30, 30, 46),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Hint content..."
            };
            Controls.Add(txtHintContent);

            numHintCost = new NumericUpDown
            {
                Location = new Point(368, y),
                Size = new Size(70, 26),
                Minimum = 0,
                Maximum = 500,
                Value = 10,
                BackColor = Color.FromArgb(30, 30, 46),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            Controls.Add(numHintCost);

            Button btnAddHint = new()
            {
                Text = "+ Add",
                Location = new Point(446, y),
                Size = new Size(70, 26),
                BackColor = Color.FromArgb(40, 80, 40),
                ForeColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btnAddHint.FlatAppearance.BorderSize = 0;
            btnAddHint.Click += BtnAddHint_Click;
            Controls.Add(btnAddHint);
            y += 34;

            lstPendingHints = new ListBox
            {
                Location = new Point(40, y),
                Size = new Size(520, 75),
                BackColor = Color.FromArgb(24, 24, 38),
                ForeColor = Color.LightGreen,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(lstPendingHints);

            Button btnRemoveHint = new()
            {
                Text = "🗑️ Remove Hint",
                Location = new Point(40, y + 81),
                Size = new Size(140, 26),
                BackColor = Color.FromArgb(60, 20, 20),
                ForeColor = Color.OrangeRed,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btnRemoveHint.FlatAppearance.BorderSize = 0;
            btnRemoveHint.Click += (s, e) =>
            {
                if (lstPendingHints.SelectedIndex < 0) return;
                _pendingHints.RemoveAt(lstPendingHints.SelectedIndex);
                lstPendingHints.Items.RemoveAt(lstPendingHints.SelectedIndex);
            };
            Controls.Add(btnRemoveHint);
            y += 115;

            // ===== FILES SECTION =====
            Controls.Add(new Label
            {
                Text = "📎 Challenge Files (optional, max 50MB each)",
                Location = new Point(40, y),
                AutoSize = true,
                ForeColor = Color.CornflowerBlue,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            });
            y += 28;

            lstPendingFiles = new ListBox
            {
                Location = new Point(40, y),
                Size = new Size(520, 70),
                BackColor = Color.FromArgb(24, 24, 38),
                ForeColor = Color.CornflowerBlue,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(lstPendingFiles);
            y += 78;

            Button btnAddFile = new()
            {
                Text = "📤 Add File",
                Location = new Point(40, y),
                Size = new Size(120, 28),
                BackColor = Color.FromArgb(30, 50, 100),
                ForeColor = Color.CornflowerBlue,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btnAddFile.FlatAppearance.BorderSize = 0;
            btnAddFile.Click += BtnAddFile_Click;
            Controls.Add(btnAddFile);

            Button btnRemoveFile = new()
            {
                Text = "🗑️ Remove File",
                Location = new Point(168, y),
                Size = new Size(130, 28),
                BackColor = Color.FromArgb(60, 20, 20),
                ForeColor = Color.OrangeRed,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btnRemoveFile.FlatAppearance.BorderSize = 0;
            btnRemoveFile.Click += (s, e) =>
            {
                if (lstPendingFiles.SelectedIndex < 0) return;
                _pendingFiles.RemoveAt(lstPendingFiles.SelectedIndex);
                lstPendingFiles.Items.RemoveAt(lstPendingFiles.SelectedIndex);
            };
            Controls.Add(btnRemoveFile);
            y += 36;

            // ===== CREATE BUTTON =====
            btnCreate = new Button
            {
                Text = "Create Challenge",
                Location = new Point(40, y),
                Size = new Size(200, 40),
                BackColor = Color.FromArgb(50, 100, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCreate.FlatAppearance.BorderSize = 0;
            btnCreate.Click += BtnCreate_Click;
            Controls.Add(btnCreate);

            lblStatus = new Label
            {
                Location = new Point(40, y + 50),
                Size = new Size(500, 24),
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.Gray,
                Text = ""
            };
            Controls.Add(lblStatus);
        }

        // ==================== EVENTS ====================

        private void BtnAddHint_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHintContent.Text)) return;
            string content = txtHintContent.Text.Trim();
            int cost = (int)numHintCost.Value;
            _pendingHints.Add((content, cost));
            lstPendingHints.Items.Add($"[{cost} pts]  {content}");
            txtHintContent.Clear();
        }

        private async void BtnAddFile_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dlg = new()
            {
                Filter = "Challenge Files|*.zip;*.tar;*.gz;*.pcap;*.pcapng;*.exe;*.elf;*.bin;*.py;*.txt;*.png;*.jpg;*.pdf",
                Title = "Select Challenge File"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            FileInfo fi = new(dlg.FileName);
            if (fi.Length > MaxFileSize)
            {
                MessageBox.Show(
                    $"File too large! Maximum size is 100MB.\nFile size: {fi.Length / 1024 / 1024}MB",
                    "File Too Large", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            byte[] data = await File.ReadAllBytesAsync(dlg.FileName);
            string fileName = Path.GetFileName(dlg.FileName);
            _pendingFiles.Add((fileName, data));
            lstPendingFiles.Items.Add($"📄  {fileName}  ({fi.Length / 1024} KB)");
        }

        private async void BtnCreate_Click(object? sender, EventArgs e)
        {
            btnCreate.Enabled = false;
            lblStatus.ForeColor = Color.Yellow;
            lblStatus.Text = "Creating...";

            int categoryId = cmbCategory.SelectedIndex + 1;

            var req = new CreateChallengeRequest
            {
                Title = txtTitle.Text.Trim(),
                Description = rtbDescription.Text.Trim(),
                Flag = txtFlag.Text.Trim(),
                Points = (int)numPoints.Value,
                Difficulty = cmbDifficulty.SelectedItem?.ToString() ?? "Easy",
                CategoryId = categoryId
            };

            var (success, message) = await _server.CreateChallengeAsync(req);

            if (success && int.TryParse(message, out int newId))
            {
                foreach (var (content, cost) in _pendingHints)
                    await _server.AddHintAsync(newId, content, cost);

                foreach (var (fileName, data) in _pendingFiles)
                    await _server.UploadFileAsync(newId, fileName, data);

                lblStatus.ForeColor = Color.LightGreen;
                lblStatus.Text = "✅ Challenge created!";
                ClearForm();
            }
            else
            {
                lblStatus.ForeColor = Color.OrangeRed;
                lblStatus.Text = $"❌ {message}";
            }

            btnCreate.Enabled = true;
        }

        private void ClearForm()
        {
            txtTitle.Clear();
            rtbDescription.Clear();
            txtFlag.Clear();
            numPoints.Value = 100;
            _pendingHints.Clear();
            lstPendingHints.Items.Clear();
            txtHintContent.Clear();
            _pendingFiles.Clear();
            lstPendingFiles.Items.Clear();
        }

        // ==================== HELPERS ====================

        private TextBox AddField(string label, int x, ref int y)
        {
            AddLabel(label, x, y);
            TextBox txt = new()
            {
                Location = new Point(x, y + 22),
                Size = new Size(520, 30),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(30, 30, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(txt);
            y += 65;
            return txt;
        }

        private RichTextBox AddRichField(string label, int x, ref int y)
        {
            AddLabel(label, x, y);
            RichTextBox rtb = new()
            {
                Location = new Point(x, y + 22),
                Size = new Size(520, 80),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(30, 30, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(rtb);
            y += 105;
            return rtb;
        }

        private void AddLabel(string text, int x, int y)
        {
            Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9)
            });
        }
    }
}