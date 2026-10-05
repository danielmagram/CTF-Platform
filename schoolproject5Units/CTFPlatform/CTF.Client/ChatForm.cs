using CTF.Common.Packets;

namespace CTF.Client
{
    public partial class ChatForm : UserControl
    {
        private readonly ServerConnection _server;
        private RichTextBox rtbChat;
        private TextBox txtMessage;
        private Button btnSend;
        private System.Windows.Forms.Timer _pollTimer;
        private int _lastMessageId = 0;
        private readonly HashSet<int> _shownIds = new();
        public ChatForm(ServerConnection server)
        {
            _server = server;
            SetupUI();
            StartPolling();
        }

        private void SetupUI()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.FromArgb(18, 18, 28);

            // Header — אחרון
            // Input panel
            Panel inputPanel = new()
            {
                Dock = DockStyle.Bottom,
                Height = 54,
                BackColor = Color.FromArgb(24, 24, 36),
                Padding = new Padding(12, 10, 12, 10)
            };

            txtMessage = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(32, 32, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Type a message and press Enter..."
            };
            txtMessage.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    _ = SendMessageAsync();
                }
            };
            inputPanel.Controls.Add(txtMessage);

            btnSend = new Button
            {
                Dock = DockStyle.Right,
                Width = 80,
                BackColor = Color.FromArgb(50, 140, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Text = "Send",
                Cursor = Cursors.Hand
            };
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += async (s, e) => await SendMessageAsync();
            inputPanel.Controls.Add(btnSend);

            Controls.Add(inputPanel);

            // Chat area — Fill
            rtbChat = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 32),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            Controls.Add(rtbChat);

            // Header — אחרון כדי להיות למעלה
            Controls.Add(new Label
            {
                Text = "💬 Live Chat",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 100),
                Dock = DockStyle.Top,
                Height = 44,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, 0, 0, 0)
            });
        }



        private void StartPolling()
        {
            _pollTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _pollTimer.Tick += async (s, e) => await PollNewMessagesAsync();
            _pollTimer.Start();
        }

        private async Task PollNewMessagesAsync()
        {
            try
            {
                var newMessages = await _server.GetNewMessagesAsync(_lastMessageId);
                foreach (var msg in newMessages)
                {
                    AppendMessage(msg);
                    _lastMessageId = msg.Id;
                }
            }
            catch { }
        }

        private async Task SendMessageAsync()
        {
            string text = txtMessage.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            txtMessage.Clear();
            btnSend.Enabled = false;
            await _server.SendChatMessageAsync(text);
            btnSend.Enabled = true;
            txtMessage.Focus();
        }
        private void AppendMessage(CTF.Common.Packets.ChatMessage msg)
        {
            if (InvokeRequired) { Invoke(() => AppendMessage(msg)); return; }

            if (msg.Id > 0 && !_shownIds.Add(msg.Id)) return;

            bool isMe = msg.Username == _server.CurrentUser?.Username;
            string time = msg.SentAt.ToLocalTime().ToString("HH:mm");

            rtbChat.SelectionStart = rtbChat.TextLength;

            rtbChat.SelectionColor = Color.FromArgb(80, 80, 110);
            rtbChat.SelectionFont = new Font("Segoe UI", 8);
            rtbChat.AppendText($"[{time}] ");

            rtbChat.SelectionColor = isMe ? Color.FromArgb(100, 200, 100) : Color.CornflowerBlue;
            rtbChat.SelectionFont = new Font("Segoe UI", 10, FontStyle.Bold);
            rtbChat.AppendText($"{msg.Username}: ");

            rtbChat.SelectionColor = Color.FromArgb(220, 220, 230);
            rtbChat.SelectionFont = new Font("Segoe UI", 10);
            rtbChat.AppendText($"{msg.Message}\n");

            rtbChat.ScrollToCaret();
        }
        public void ReceiveBroadcast(string message) { }

        protected override void Dispose(bool disposing)
        {
            _pollTimer?.Stop();
            _pollTimer?.Dispose();
            base.Dispose(disposing);
        }
    }
}