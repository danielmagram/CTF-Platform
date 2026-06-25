namespace CTF.Client
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            ServerConnection server = new("127.0.0.1", 5000);
            Application.Run(new LoginForm(server));
        }
    }
}