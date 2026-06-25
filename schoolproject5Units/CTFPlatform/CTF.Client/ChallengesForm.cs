using CTF.Common.Models;

namespace CTF.Client
{
    public partial class ChallengesForm : UserControl
    {
        private readonly ServerConnection _server;
        private List<Challenge> _challenges = new();
        private Challenge? _selected;

        private ListBox lstChallenges;
        private ComboBox cmbCategory, cmbDifficulty;
        private Label lblTitle, lblCategory, lblDifficulty, lblPoints, lblResult;
        private RichTextBox rtbDescription;
        private TextBox txtFlag;
        private Button btnSubmit, btnHints;

        public ChallengesForm(ServerConnection server)
        {
            _server = server;
            SetupUI();
            _ = LoadChallengesAsync();
        }

        private void SetupUI()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(18, 18, 28);

            TableLayoutPanel table = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(table);

            table.Controls.Add(BuildLeftPanel(), 0, 0);
            table.Controls.Add(BuildRightPanel(), 1, 0);
        }

        // ===== LEFT PANEL =====
        private Panel BuildLeftPanel()
        {
            Panel left = new()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(22, 22, 34),
                Padding = new Padding(10)
            };

            // 1.first Fill 
            lstChallenges = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(22, 22, 34),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 52
            };
            lstChallenges.DrawItem += DrawChallengeItem;
            lstChallenges.SelectedIndexChanged += OnChallengeSelected;
            left.Controls.Add(lstChallenges);

            // 2. Filters
            Panel filterPanel = new()
            {
                Dock = DockStyle.Top,
                Height = 34,
                BackColor = Color.Transparent
            };

            cmbCategory = new ComboBox
            {
                Location = new Point(0, 4),
                Size = new Size(118, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(32, 32, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8)
            };
            cmbCategory.Items.Add("All");
            cmbCategory.Items.AddRange(new[] { "Crypto", "Web", "Forensics", "Reversing", "Pwn", "Misc", "Other" });
            cmbCategory.SelectedIndex = 0;
            cmbCategory.SelectedIndexChanged += (s, e) => FilterChallenges();
            filterPanel.Controls.Add(cmbCategory);

            cmbDifficulty = new ComboBox
            {
                Location = new Point(122, 4),
                Size = new Size(118, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(32, 32, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8)
            };
            cmbDifficulty.Items.Add("All");
            cmbDifficulty.Items.AddRange(new[] { "Easy", "Medium", "Hard", "Insane" });
            cmbDifficulty.SelectedIndex = 0;
            cmbDifficulty.SelectedIndexChanged += (s, e) => FilterChallenges();
            filterPanel.Controls.Add(cmbDifficulty);
            left.Controls.Add(filterPanel);

            // 3. Header last
            left.Controls.Add(new Label
            {
                Text = "🏆 Challenges",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 100),
                Dock = DockStyle.Top,
                Height = 36,
                TextAlign = ContentAlignment.MiddleLeft
            });

            return left;
        }

        // ===== RIGHT PANEL =====
        private Panel BuildRightPanel()
        {
            Panel right = new()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(18, 18, 28),
                Padding = new Padding(30, 20, 30, 20)
            };

            // Title
            lblTitle = new Label
            {
                Text = "← Select a challenge from the list",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 160, 180),
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft
            };
            right.Controls.Add(lblTitle);

            // Tags
            Panel tagsPanel = new()
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.Transparent
            };
            lblCategory = MakeTag(0, 6, Color.FromArgb(40, 80, 160));
            lblDifficulty = MakeTag(110, 6, Color.FromArgb(70, 40, 110));
            lblPoints = MakeTag(220, 6, Color.FromArgb(40, 100, 40));
            tagsPanel.Controls.Add(lblCategory);
            tagsPanel.Controls.Add(lblDifficulty);
            tagsPanel.Controls.Add(lblPoints);
            right.Controls.Add(tagsPanel);

            // Description
            Panel descPanel = new()
            {
                Dock = DockStyle.Top,
                Height = 220,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 8)
            };
            rtbDescription = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 24, 38),
                ForeColor = Color.FromArgb(200, 200, 220),
                Font = new Font("Consolas", 10),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Padding = new Padding(8)
            };
            descPanel.Controls.Add(rtbDescription);
            right.Controls.Add(descPanel);

            // Flag label
            right.Controls.Add(new Label
            {
                Text = "Submit Flag:",
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.Gray,
                TextAlign = ContentAlignment.BottomLeft
            });

            // Flag input row
            Panel flagPanel = new()
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.Transparent
            };
            txtFlag = new TextBox
            {
                Location = new Point(0, 4),
                Size = new Size(420, 30),
                Font = new Font("Consolas", 11),
                BackColor = Color.FromArgb(28, 28, 44),
                ForeColor = Color.LightGreen,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "CTF{...}"
            };
            flagPanel.Controls.Add(txtFlag);

            btnSubmit = new Button
            {
                Text = "Submit",
                Location = new Point(428, 4),
                Size = new Size(90, 30),
                BackColor = Color.FromArgb(45, 135, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSubmit.FlatAppearance.BorderSize = 0;
            btnSubmit.Click += BtnSubmit_Click;
            flagPanel.Controls.Add(btnSubmit);
            right.Controls.Add(flagPanel);

            // Result
            lblResult = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.Gray,
                Text = "",
                TextAlign = ContentAlignment.MiddleLeft
            };
            right.Controls.Add(lblResult);

            // Buttons row — Hints + Download
            Panel actionsPanel = new()
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.Transparent
            };

            btnHints = new Button
            {
                Text = "💡 Hints",
                Location = new Point(0, 4),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(45, 45, 12),
                ForeColor = Color.Yellow,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btnHints.FlatAppearance.BorderSize = 0;
            btnHints.Click += BtnHints_Click;
            actionsPanel.Controls.Add(btnHints);

            Button btnDownload = new()
            {
                Text = "📥 Files",
                Location = new Point(108, 4),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(20, 50, 80),
                ForeColor = Color.CornflowerBlue,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btnDownload.FlatAppearance.BorderSize = 0;
            btnDownload.Click += BtnDownload_Click;
            actionsPanel.Controls.Add(btnDownload);

            right.Controls.Add(actionsPanel);

            // Edit/Delete only to Admin/Creator
            UserRole role = _server.CurrentUser?.Role ?? UserRole.Player;
            if (role == UserRole.Admin || role == UserRole.Creator)
            {
                Panel adminPanel = new()
                {
                    Dock = DockStyle.Top,
                    Height = 38,
                    BackColor = Color.Transparent
                };

                Button btnEdit = new()
                {
                    Text = "✏️ Edit",
                    Location = new Point(0, 4),
                    Size = new Size(100, 30),
                    BackColor = Color.FromArgb(40, 80, 130),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9),
                    Cursor = Cursors.Hand
                };
                btnEdit.FlatAppearance.BorderSize = 0;
                btnEdit.Click += BtnEdit_Click;
                adminPanel.Controls.Add(btnEdit);

                if (role == UserRole.Admin)
                {
                    Button btnDelete = new()
                    {
                        Text = "🗑️ Delete",
                        Location = new Point(108, 4),
                        Size = new Size(100, 30),
                        BackColor = Color.FromArgb(100, 30, 30),
                        ForeColor = Color.OrangeRed,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 9),
                        Cursor = Cursors.Hand
                    };
                    btnDelete.FlatAppearance.BorderSize = 0;
                    btnDelete.Click += BtnDelete_Click;
                    adminPanel.Controls.Add(btnDelete);
                }

                right.Controls.Add(adminPanel);
            }

            return right;
        }

        // ==================== LOAD ====================

        private async Task LoadChallengesAsync()
        {
            _challenges = await _server.GetChallengesAsync();
            if (InvokeRequired) Invoke(FilterChallenges);
            else FilterChallenges();
        }

        private void FilterChallenges()
        {
            string cat = cmbCategory.SelectedItem?.ToString() ?? "All";
            string diff = cmbDifficulty.SelectedItem?.ToString() ?? "All";

            var filtered = _challenges
                .Where(c => cat == "All" || c.CategoryName == cat)
                .Where(c => diff == "All" || c.Difficulty.ToString() == diff)
                .ToList();

            lstChallenges.Items.Clear();
            foreach (Challenge c in filtered)
                lstChallenges.Items.Add(c);
        }

        // ==================== EVENTS ====================

        private void OnChallengeSelected(object? sender, EventArgs e)
        {
            if (lstChallenges.SelectedItem is not Challenge c) return;
            _selected = c;

            lblTitle.Text = c.Title;
            lblTitle.ForeColor = Color.White;
            lblCategory.Text = $"  {c.CategoryName}  ";
            lblDifficulty.Text = $"  {c.Difficulty}  ";
            lblDifficulty.BackColor = DifficultyColor(c.Difficulty);
            lblPoints.Text = $"  {c.Points} pts  ";
            rtbDescription.Text = c.Description;
            lblResult.Text = "";
            txtFlag.Clear();
        }

        private async void BtnSubmit_Click(object? sender, EventArgs e)
        {
            if (_selected == null || string.IsNullOrWhiteSpace(txtFlag.Text)) return;

            btnSubmit.Enabled = false;
            lblResult.ForeColor = Color.Yellow;
            lblResult.Text = "Checking...";

            var (success, data, message) = await _server.SubmitFlagAsync(
                _selected.Id, txtFlag.Text.Trim());

            if (success && data != null)
            {
                lblResult.ForeColor = data.IsCorrect ? Color.LightGreen : Color.OrangeRed;
                lblResult.Text = data.IsCorrect
                    ? $"🎉 Correct! +{data.PointsEarned} pts (Total: {data.NewScore})"
                    : "❌ Wrong flag, try again.";
            }
            else
            {
                lblResult.ForeColor = Color.OrangeRed;
                lblResult.Text = message;
            }

            btnSubmit.Enabled = true;
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            if (_selected == null) return;
            EditChallengeForm editForm = new(_server, _selected);
            editForm.FormClosed += async (s, e) => await LoadChallengesAsync();
            editForm.ShowDialog();
        }

        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_selected == null) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete '{_selected.Title}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            var (success, message) = await _server.DeleteChallengeAsync(_selected.Id);
            lblResult.ForeColor = success ? Color.LightGreen : Color.OrangeRed;
            lblResult.Text = success ? "✅ Challenge deleted!" : $"❌ {message}";

            if (success) await LoadChallengesAsync();
        }

        private async void BtnHints_Click(object? sender, EventArgs e)
        {
            if (_selected == null) return;

            var hints = await _server.GetHintsAsync(_selected.Id);
            if (hints.Count == 0)
            {
                MessageBox.Show("No hints for this challenge.", "Hints",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            HintsForm hintsForm = new(_server, _selected, hints);
            hintsForm.ShowDialog();
        }

        private async void BtnDownload_Click(object? sender, EventArgs e)
        {
            if (_selected == null) return;

            var files = await _server.GetChallengeFilesAsync(_selected.Id);
            if (files.Count == 0)
            {
                MessageBox.Show("No files for this challenge.", "Files",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // choose file
            ChallengeFile fileToDownload;
            if (files.Count == 1)
            {
                fileToDownload = files[0];
            }
            else
            {
                // show options
                string fileList = string.Join("\n", files.Select((f, i) => $"{i + 1}. {f.FileName}"));
                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    $"Select file number:\n{fileList}", "Select File", "1");

                if (!int.TryParse(input, out int idx) || idx < 1 || idx > files.Count) return;
                fileToDownload = files[idx - 1];
            }

            // download
            var data = await _server.DownloadFileAsync(fileToDownload.Id);
            if (data == null)
            {
                MessageBox.Show("Download failed!", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using SaveFileDialog dlg = new()
            {
                FileName = fileToDownload.FileName,
                Title = "Save File"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            await File.WriteAllBytesAsync(dlg.FileName, data.FileData);
            MessageBox.Show("✅ File downloaded!", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ==================== DRAWING ====================

        private void DrawChallengeItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || lstChallenges.Items[e.Index] is not Challenge c) return;

            e.DrawBackground();
            bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using SolidBrush bgBrush = new(sel ? Color.FromArgb(28, 48, 28) : Color.FromArgb(22, 22, 34));
            e.Graphics.FillRectangle(bgBrush, e.Bounds);

            using SolidBrush barBrush = new(DifficultyColor(c.Difficulty));
            e.Graphics.FillRectangle(barBrush,
                new Rectangle(e.Bounds.X, e.Bounds.Y + 4, 4, e.Bounds.Height - 8));

            using SolidBrush whiteBrush = new(Color.White);
            e.Graphics.DrawString(c.Title,
                new Font("Segoe UI", 9, FontStyle.Bold), whiteBrush,
                new RectangleF(e.Bounds.X + 14, e.Bounds.Y + 8, e.Bounds.Width - 18, 22));

            using SolidBrush grayBrush = new(Color.FromArgb(120, 120, 145));
            e.Graphics.DrawString($"{c.CategoryName}  •  {c.Points} pts",
                new Font("Segoe UI", 8), grayBrush,
                new RectangleF(e.Bounds.X + 14, e.Bounds.Y + 30, e.Bounds.Width - 18, 18));

            e.DrawFocusRectangle();
        }

        // ==================== HELPERS ====================

        private static Label MakeTag(int x, int y, Color bg) => new()
        {
            Location = new Point(x, y),
            AutoSize = true,
            BackColor = bg,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            Padding = new Padding(6, 3, 6, 3),
            Text = ""
        };

        private static Color DifficultyColor(DifficultyLevel d) => d switch
        {
            DifficultyLevel.Easy => Color.MediumSeaGreen,
            DifficultyLevel.Medium => Color.Goldenrod,
            DifficultyLevel.Hard => Color.OrangeRed,
            DifficultyLevel.Insane => Color.MediumOrchid,
            _ => Color.Gray
        };
    }
}