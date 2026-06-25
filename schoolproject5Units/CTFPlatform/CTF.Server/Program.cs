using CTF.Server;

Console.Title = "CTF Platform Server";
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine(@"
█▀▀ ▀█▀ █▀▀   █▀ █▀▀ █▀█ █░█ █▀▀ █▀█
█▄▄ ░█░ █▀░   ▄█ ██▄ █▀▄ ▀▄▀ ██▄ █▀▄
");
Console.ResetColor();

TcpServer server = new(port: 5000);
await server.StartAsync();