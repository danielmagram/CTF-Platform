using CTF.Common.Models;
using CTF.Common.Packets;
using System.IO;

namespace CTF.Client
{
    public partial class EditChallengeForm : Form
    {
        private readonly ServerConnection _server;
        private readonly Challenge _challenge;

        private TextBox txtTitle, txtFlag;
        private RichTextBox rtbDescription;
        private ComboBox cmbCategory, cmbDifficulty;
        private NumericUpDown numPoints;
        private CheckBox chkActive;
        private Button btnSave;
        private Label lblStatus;

        // Hints
        private ListBox lstHints;
        private TextBox txtHintContent;
        private NumericUpDown numHintCost;
        private List<Hint> _currentHints = new();

        // Files
        private ListBox lstFiles;
        private List<ChallengeFile> _currentFiles = new();

        public EditChallengeForm(ServerConnection server, Challenge challenge)
        {
            _server = server;
            _challenge = challenge;
            SetupUI();
            PopulateFields();
            _ = LoadHintsAsync();
            _ = LoadFilesAsync();
        }

        private void SetupUI()
        {
            Text = $"Edit — {_challenge.Title}";
            Size = new Size(580, 900);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.FromArgb(22, 22, 34);
            AutoScroll = true;

            Controls.Add(new Label
            {
                Text = "✏️ Edit Challenge",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.CornflowerBlue,
                Location = new Point(20, 15),
                Size = new Size(500, 28)
            });

            int y = 55;

            // ===== CHALLENGE FIELDS =====
            txtTitle = AddField("Title", 20, ref y, 520);
            rtbDescription = AddRichField("Description", 20, ref y, 520);
            txtFlag = AddField("New Flag (leave empty to keep current)", 20, ref y, 520);

            AddLabel("Category", 20, y);
            cmbCategory = new ComboBox
            {
                Location = new Point(20, y + 20),
                Size = new Size(150, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(32, 32, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbCategory.Items.AddRange(new[] { "Crypto", "Web", "Forensics", "Reversing", "Pwn", "Misc", "Other" });
            Controls.Add(cmbCategory);

            AddLabel("Difficulty", 180, y);
            cmbDifficulty = new ComboBox
            {
                Location = new Point(180, y + 20),
                Size = new Size(120, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(32, 32, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbDifficulty.Items.AddRange(new[] { "Easy", "Medium", "Hard", "Insane" });
            Controls.Add(cmbDifficulty);

            AddLabel("Points", 315, y);
            numPoints = new NumericUpDown
            {
                Location = new Point(315, y + 20),
                Size = new Size(90, 26),
                Minimum = 10,
                Maximum = 1000,
                Increment = 10,
                BackColor = Color.FromArgb(32, 32, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10)
            };
            Controls.Add(numPoints);

            chkActive = new CheckBox
            {
                Text = "Active",
                Location = new Point(420, y + 22),
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Checked = true
            };
            Controls.Add(chkActive);
            y += 55;

            // Save button
            btnSave = new Button
            {
                Text = "💾 Save Changes",
                Location = new Point(20, y),
                Size = new Size(180, 38),
                BackColor = Color.FromArgb(40, 100, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            lblStatus = new Label
            {
                Location = new Point(20, y + 46),
                Size = new Size(520, 22),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Gray,
                Text = ""
            };
            Controls.Add(lblStatus);
            y += 76;

            // ===== HINTS SECTION =====
            AddSectionHeader("💡 Hints", Color.Yellow, 20, y);
            y += 28;

            lstHints = new ListBox
            {
                Location = new Point(20, y),
                Size = new Size(520, 85),
                BackColor = Color.FromArgb(24, 24, 38),
                ForeColor = Color.LightGreen,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(lstHints);
            y += 93;

            Button btnDeleteHint = MakeSmallButton("🗑️ Delete Hint", Color.FromArgb(60, 20, 20), Color.OrangeRed, 20, y);
            btnDeleteHint.Click += BtnDeleteHint_Click;
            Controls.Add(btnDeleteHint);
            y += 34;

            AddLabel("Add Hint:", 20, y);
            y += 20;

            txtHintContent = new TextBox
            {
                Location = new Point(20, y),
                Size = new Size(310, 26),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(30, 30, 46),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Hint content..."
            };
            Controls.Add(txtHintContent);

            numHintCost = new NumericUpDown
            {
                Location = new Point(338, y),
                Size = new Size(70, 26),
                Minimum = 0,
                Maximum = 500,
                Value = 10,
                BackColor = Color.FromArgb(30, 30, 46),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            Controls.Add(numHintCost);

            Button btnAddHint = MakeSmallButton("+ Add", Color.FromArgb(40, 80, 40), Color.LightGreen, 416, y);
            btnAddHint.Click += BtnAddHint_Click;
            Controls.Add(btnAddHint);
            y += 40;

            // ===== FILES SECTION =====
            AddSectionHeader("📎 Challenge Files", Color.CornflowerBlue, 20, y);
            y += 28;

            lstFiles = new ListBox
            {
                Location = new Point(20, y),
                Size = new Size(520, 85),
                BackColor = Color.FromArgb(24, 24, 38),
                ForeColor = Color.CornflowerBlue,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(lstFiles);
            y += 93;

            Button btnUpload = MakeSmallButton("📤 Upload File", Color.FromArgb(30, 50, 100), Color.CornflowerBlue, 20, y);
            btnUpload.Click += BtnUpload_Click;
            Controls.Add(btnUpload);

            Button btnDownload = MakeSmallButton("📥 Download", Color.FromArgb(20, 50, 30), Color.LightGreen, 160, y);
            btnDownload.Click += BtnDownloadFile_Click;
            Controls.Add(btnDownload);
            Button btnDeleteFile = MakeSmallButton("🗑️ Delete File", Color.FromArgb(60, 20, 20), Color.OrangeRed, 300, y);
            btnDeleteFile.Click += BtnDeleteFile_Click;
            Controls.Add(btnDeleteFile);
        }

        private void PopulateFields()
        {
            txtTitle.Text = _challenge.Title;
            rtbDescription.Text = _challenge.Description;
            numPoints.Value = _challenge.Points;
            cmbDifficulty.SelectedItem = _challenge.Difficulty.ToString();
            int catIndex = cmbCategory.Items.IndexOf(_challenge.CategoryName);
            cmbCategory.SelectedIndex = catIndex >= 0 ? catIndex : 0;
            chkActive.Checked = _challenge.IsActive;
        }

        private async Task LoadHintsAsync()
        {
            _currentHints = await _server.GetHintsByChallengeAsync(_challenge.Id);
            if (InvokeRequired) Invoke(RefreshHintsList);
            else RefreshHintsList();
        }

        private void RefreshHintsList()
        {
            lstHints.Items.Clear();
            foreach (Hint h in _currentHints)
                lstHints.Items.Add($"[{h.PointCost} pts]  {h.Content}");
        }

        private async Task LoadFilesAsync()
        {
            _currentFiles = await _server.GetChallengeFilesAsync(_challenge.Id);
            if (InvokeRequired) Invoke(RefreshFilesList);
            else RefreshFilesList();
        }

        private void RefreshFilesList()
        {
            lstFiles.Items.Clear();
            foreach (ChallengeFile f in _currentFiles)
                lstFiles.Items.Add($"📄  {f.FileName}");
        }

        // ==================== EVENTS ====================

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            btnSave.Enabled = false;
            lblStatus.ForeColor = Color.Yellow;
            lblStatus.Text = "Saving...";

            var req = new UpdateChallengeRequest
            {
                Id = _challenge.Id,
                Title = txtTitle.Text.Trim(),
                Description = rtbDescription.Text.Trim(),
                Flag = txtFlag.Text.Trim(),
                Points = (int)numPoints.Value,
                Difficulty = cmbDifficulty.SelectedItem?.ToString() ?? "Easy",
                CategoryId = cmbCategory.SelectedIndex + 1,
                IsActive = chkActive.Checked
            };

            var (success, message) = await _server.UpdateChallengeAsync(req);
            lblStatus.ForeColor = success ? Color.LightGreen : Color.OrangeRed;
            lblStatus.Text = success ? "✅ Saved!" : $"❌ {message}";

            if (success) { await Task.Delay(800); Close(); }
            btnSave.Enabled = true;
        }

        private async void BtnAddHint_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtHintContent.Text)) return;
            var (success, message) = await _server.AddHintAsync(
                _challenge.Id, txtHintContent.Text.Trim(), (int)numHintCost.Value);
            if (success) { txtHintContent.Clear(); await LoadHintsAsync(); }
            else MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private async void BtnDeleteHint_Click(object? sender, EventArgs e)
        {
            if (lstHints.SelectedIndex < 0) return;
            Hint selected = _currentHints[lstHints.SelectedIndex];
            var (success, message) = await _server.DeleteHintAsync(selected.Id);
            if (success) await LoadHintsAsync();
            else MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private async void BtnUpload_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dlg = new()
            {
                Filter = "Challenge Files|*.zip;*.tar;*.gz;*.pcap;*.pcapng;*.exe;*.elf;*.bin;*.py;*.txt;*.png;*.jpg;*.pdf",
                Title = "Select Challenge File"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            const long MaxFileSize = 100 * 1024 * 1024;

            byte[] fileData = await File.ReadAllBytesAsync(dlg.FileName);
            string fileName = Path.GetFileName(dlg.FileName);
            if (fileData.Length > MaxFileSize) 
            {
                MessageBox.Show($"File too large! Maximum size is 100MB.\nFile size: {fileData.Length / 1024 / 1024}MB",
    "File Too Large", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; 
            }
            var (success, message) = await _server.UploadFileAsync(_challenge.Id, fileName, fileData);
            if (success) await LoadFilesAsync();
            else MessageBox.Show(message, "Upload Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private async void BtnDeleteFile_Click(object? sender, EventArgs e)
        {
            if (lstFiles.SelectedIndex < 0)
            {
                MessageBox.Show("Select a file first.", "Delete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ChallengeFile selected = _currentFiles[lstFiles.SelectedIndex];
            var confirm = MessageBox.Show(
                $"Delete '{selected.FileName}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            var (success, message) = await _server.DeleteFileAsync(selected.Id);
            if (success) await LoadFilesAsync();
            else MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        private async void BtnDownloadFile_Click(object? sender, EventArgs e)
        {
            if (lstFiles.SelectedIndex < 0)
            {
                MessageBox.Show("Select a file first.", "Download",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ChallengeFile selected = _currentFiles[lstFiles.SelectedIndex];
            var data = await _server.DownloadFileAsync(selected.Id);
            if (data == null)
            {
                MessageBox.Show("Download failed!", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using SaveFileDialog dlg = new() { FileName = selected.FileName, Title = "Save File" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            await File.WriteAllBytesAsync(dlg.FileName, data.FileData);
            MessageBox.Show("✅ Downloaded!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ==================== HELPERS ====================

        private void AddSectionHeader(string text, Color color, int x, int y)
        {
            Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                ForeColor = color,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            });
        }

        private static Button MakeSmallButton(string text, Color bg, Color fg, int x, int y)
        {
            Button btn = new()
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(130, 28),
                BackColor = bg,
                ForeColor = fg,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private TextBox AddField(string label, int x, ref int y, int width)
        {
            AddLabel(label, x, y);
            TextBox txt = new()
            {
                Location = new Point(x, y + 20),
                Size = new Size(width, 28),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(30, 30, 46),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(txt);
            y += 58;
            return txt;
        }

        private RichTextBox AddRichField(string label, int x, ref int y, int width)
        {
            AddLabel(label, x, y);
            RichTextBox rtb = new()
            {
                Location = new Point(x, y + 20),
                Size = new Size(width, 75),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(30, 30, 46),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(rtb);
            y += 100;
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
                Font = new Font("Segoe UI", 8)
            });
        }
    }
}