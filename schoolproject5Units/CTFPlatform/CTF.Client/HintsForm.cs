using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using CTF.Common.Models;

namespace CTF.Client
{
    public partial class HintsForm : Form
    {
        private readonly ServerConnection _server;
        private readonly Challenge _challenge;
        private List<Hint> _hints;

        public HintsForm(ServerConnection server, Challenge challenge, List<Hint> hints)
        {
            _server = server;
            _challenge = challenge;
            _hints = hints;
            SetupUI();
        }

        private void SetupUI()
        {
            Text = $"💡 Hints — {_challenge.Title}";
            Size = new Size(500, 400);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(22, 22, 34);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            Controls.Add(new Label
            {
                Text = $"💡 Hints for: {_challenge.Title}",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.Yellow,
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0)
            });

            Panel hintsPanel = new()
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(15)
            };
            Controls.Add(hintsPanel);

            BuildHintCards(hintsPanel);
        }

        private void BuildHintCards(Panel container)
        {
            container.Controls.Clear();
            int y = 10;

            for (int i = 0; i < _hints.Count; i++)
            {
                Hint hint = _hints[i];
                Panel card = new()
                {
                    Location = new Point(0, y),
                    Size = new Size(440, hint.IsUnlocked ? 100 : 70),
                    BackColor = Color.FromArgb(28, 28, 44),
                    Padding = new Padding(12)
                };

                card.Controls.Add(new Label
                {
                    Text = $"Hint {i + 1} — Cost: {hint.PointCost} pts",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = hint.IsUnlocked ? Color.Yellow : Color.Gray,
                    Location = new Point(12, 8),
                    AutoSize = true
                });

                if (hint.IsUnlocked)
                {
                    card.Controls.Add(new Label
                    {
                        Text = hint.Content,
                        Font = new Font("Segoe UI", 10),
                        ForeColor = Color.White,
                        Location = new Point(12, 30),
                        Size = new Size(380, 40),
                        AutoSize = false
                    });
                }
                else
                {
                    Button btnUnlock = new()
                    {
                        Text = $"🔓 Unlock (-{hint.PointCost} pts)",
                        Location = new Point(12, 32),
                        Size = new Size(180, 28),
                        BackColor = Color.FromArgb(60, 50, 10),
                        ForeColor = Color.Yellow,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 9),
                        Cursor = Cursors.Hand,
                        Tag = hint
                    };
                    btnUnlock.FlatAppearance.BorderSize = 0;
                    btnUnlock.Click += BtnUnlock_Click;
                    card.Controls.Add(btnUnlock);
                }

                container.Controls.Add(card);
                y += card.Height + 10;
            }
        }

        private async void BtnUnlock_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not Hint hint) return;

            btn.Enabled = false;

            var (success, message, content, newScore) =
                await _server.UnlockHintAsync(hint.Id, _challenge.Id);

            if (success)
            {
                // רענן רשימת hints
                _hints = await _server.GetHintsAsync(_challenge.Id);
                Panel container = (Panel)Controls.OfType<Panel>().First();
                BuildHintCards(container);
            }
            else
            {
                MessageBox.Show(message, "Cannot Unlock",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btn.Enabled = true;
            }
        }
    }
}
