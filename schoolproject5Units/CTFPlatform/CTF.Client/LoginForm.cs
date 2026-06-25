using CTF.Common.Packets;

namespace CTF.Client
{
    public partial class LoginForm : Form
    {
        private readonly ServerConnection _server;

        private TabControl tabControl;
        private TabPage tabLogin, tabRegister;

        private TextBox txtLoginUsername, txtLoginPassword;
        private Button btnLogin;
        private Label lblLoginStatus;

        private TextBox txtRegUsername, txtRegPassword, txtRegConfirm;
        private Button btnRegister;
        private Label lblRegStatus;

        public LoginForm(ServerConnection server)
        {
            _server = server;
            SetupUI();
        }

        private void SetupUI()
        {
            Text = "CTF Platform — Login";
            Size = new Size(420, 500);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.FromArgb(18, 18, 28);

            Controls.Add(new Label
            {
                Text = "🚩 CTF Platform",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 200, 100),
                AutoSize = true,
                Location = new Point(100, 20)
            });

            tabControl = new TabControl
            {
                Location = new Point(20, 70),
                Size = new Size(360, 370),
                Font = new Font("Segoe UI", 10)
            };
            Controls.Add(tabControl);

            BuildLoginTab();
            BuildRegisterTab();
        }

        private void BuildLoginTab()
        {
            tabLogin = new TabPage("Login");
            tabLogin.BackColor = Color.FromArgb(28, 28, 40);
            tabControl.TabPages.Add(tabLogin);

            tabLogin.Controls.Add(MakeLabel("Username", 15, 20));
            txtLoginUsername = MakeTextBox(15, 40);
            tabLogin.Controls.Add(txtLoginUsername);
            tabLogin.Controls.Add(MakeLabel("Password", 15, 80));
            txtLoginPassword = MakeTextBox(15, 100, isPassword: true);
            tabLogin.Controls.Add(txtLoginPassword);

            btnLogin = MakeButton("Login", 15, 145, Color.FromArgb(50, 150, 50));
            btnLogin.Click += BtnLogin_Click;
            tabLogin.Controls.Add(btnLogin);

            lblLoginStatus = MakeStatusLabel(15, 200);
            tabLogin.Controls.Add(lblLoginStatus);
        }

        private void BuildRegisterTab()
        {
            tabRegister = new TabPage("Register");
            tabRegister.BackColor = Color.FromArgb(28, 28, 40);
            tabControl.TabPages.Add(tabRegister);

            tabRegister.Controls.Add(MakeLabel("Username", 15, 15));
            txtRegUsername = MakeTextBox(15, 35);
            tabRegister.Controls.Add(txtRegUsername);

            tabRegister.Controls.Add(MakeLabel("Password", 15, 75));
            txtRegPassword = MakeTextBox(15, 95, isPassword: true);
            tabRegister.Controls.Add(txtRegPassword);

            tabRegister.Controls.Add(MakeLabel("Confirm Password", 15, 135));
            txtRegConfirm = MakeTextBox(15, 155, isPassword: true);
            tabRegister.Controls.Add(txtRegConfirm);

            btnRegister = MakeButton("Register", 15, 200, Color.FromArgb(50, 100, 180));
            btnRegister.Click += BtnRegister_Click;
            tabRegister.Controls.Add(btnRegister);

            lblRegStatus = MakeStatusLabel(15, 250);
            tabRegister.Controls.Add(lblRegStatus);
        }

        // ==================== EVENTS ====================

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            btnLogin.Enabled = false;
            lblLoginStatus.ForeColor = Color.Yellow;
            lblLoginStatus.Text = "Connecting...";

            try
            {
                if (!_server.IsConnected)
                    await _server.ConnectAsync();

                var (success, message, data) = await _server.LoginAsync(
                    txtLoginUsername.Text.Trim(),
                    txtLoginPassword.Text);

                if (success && data != null)
                {
                    lblLoginStatus.ForeColor = Color.LightGreen;
                    lblLoginStatus.Text = $"Welcome, {data.Username}!";
                    await Task.Delay(500);
                    new MainForm(_server).Show();
                    Hide();
                }
                else
                {
                    lblLoginStatus.ForeColor = Color.OrangeRed;
                    lblLoginStatus.Text = message;
                    btnLogin.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                lblLoginStatus.ForeColor = Color.OrangeRed;
                lblLoginStatus.Text = $"Connection error: {ex.Message}";
                btnLogin.Enabled = true;
            }
        }

        private async void BtnRegister_Click(object? sender, EventArgs e)
        {
            if (txtRegPassword.Text != txtRegConfirm.Text)
            {
                lblRegStatus.ForeColor = Color.OrangeRed;
                lblRegStatus.Text = "Passwords do not match!";
                return;
            }

            btnRegister.Enabled = false;
            lblRegStatus.ForeColor = Color.Yellow;
            lblRegStatus.Text = "Registering...";

            try
            {
                if (!_server.IsConnected)
                    await _server.ConnectAsync();

                var (success, message) = await _server.RegisterAsync(
                    txtRegUsername.Text.Trim(),
                    txtRegPassword.Text);

                lblRegStatus.ForeColor = success ? Color.LightGreen : Color.OrangeRed;
                lblRegStatus.Text = message;

                if (success)
                    tabControl.SelectedTab = tabLogin;
            }
            catch (Exception ex)
            {
                lblRegStatus.ForeColor = Color.OrangeRed;
                lblRegStatus.Text = $"Error: {ex.Message}";
            }
            finally
            {
                btnRegister.Enabled = true;
            }
        }

        // ==================== HELPERS ====================

        private static Label MakeLabel(string text, int x, int y) => new()
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 9)
        };

        private static TextBox MakeTextBox(int x, int y, bool isPassword = false) => new()
        {
            Location = new Point(x, y),
            Size = new Size(310, 28),
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(40, 40, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            UseSystemPasswordChar = isPassword
        };

        private static Button MakeButton(string text, int x, int y, Color color)
        {
            Button btn = new()
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(310, 40),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private static Label MakeStatusLabel(int x, int y) => new()
        {
            Location = new Point(x, y),
            Size = new Size(310, 22),
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.Gray,
            Text = ""
        };

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            Application.Exit();
        }
    }
}