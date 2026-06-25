using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using CTF.Common.Packets;
using CTF.Server.Database;

namespace CTF.Server
{
    public class TcpServer
    {
        private readonly int _port;
        private readonly DatabaseService _db;
        private readonly SessionManager _sessions;
        private readonly RequestHandler _handler;
        private TcpListener? _listener;

        private readonly List<TcpClient> _connectedClients = new();
        private readonly object _clientsLock = new();

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            MaxDepth = 64
        };

        public delegate void LogHandler(string message);
        public event LogHandler? OnLog;

        public TcpServer(int port = 5000)
        {
            _port = port;
            _db = new DatabaseService();
            _sessions = new SessionManager();
            _handler = new RequestHandler(_db, _sessions);

            _handler.OnScoreChanged += (username, newScore) =>
            {
                Log($"[SCORE] {username} → {newScore} pts");
            };

            _handler.OnChallengeCreated += (title) =>
            {
                Log($"[NEW CHALLENGE] {title}");
            };
        }

        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            Log($"[SERVER] Listening on port {_port}...");

            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                Log($"[SERVER] Client connected: {client.Client.RemoteEndPoint}");

                lock (_clientsLock)
                    _connectedClients.Add(client);

                _ = Task.Run(() => HandleClientAsync(client));
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using NetworkStream stream = client.GetStream();

            try
            {
                while (client.Connected)
                {
                    // read the message length
                    byte[] lengthBytes = new byte[4];
                    int bytesRead = await stream.ReadAsync(lengthBytes, 0, 4);
                    if (bytesRead == 0) break;

                    int messageLength = BitConverter.ToInt32(lengthBytes, 0);
                    if (messageLength <= 0 || messageLength > 100 * 1024 * 1024) break;

                    // read data itself
                    byte[] messageBytes = new byte[messageLength];
                    int totalRead = 0;
                    while (totalRead < messageLength)
                    {
                        int read = await stream.ReadAsync(
                            messageBytes, totalRead, messageLength - totalRead);
                        if (read == 0) break;
                        totalRead += read;
                    }

                    string json = Encoding.UTF8.GetString(messageBytes, 0, totalRead);
                    Packet? packet = JsonSerializer.Deserialize<Packet>(json, _jsonOptions);
                    if (packet == null) continue;

                    Log($"[REQUEST] {packet.Type}");

                    Response response;
                    try
                    {
                        response = await Task.Run(() => _handler.Handle(packet));
                        Log($"[RESPONSE] {response.Success} - {response.Message}");
                    }
                    catch (Exception ex)
                    {
                        Log($"[HANDLER ERROR] {ex.Message}");
                        response = new Response { Success = false, Message = "Server error" };
                    }

                    try
                    {
                        await SendResponseAsync(stream, response);
                        Log($"[SENT] Response sent successfully");
                    }
                    catch (Exception ex)
                    {
                        Log($"[SEND ERROR] {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Client error: {ex.Message}");
            }
            finally
            {
                lock (_clientsLock)
                    _connectedClients.Remove(client);
                client.Close();
                Log($"[SERVER] Client disconnected");
            }
        }

        private static async Task SendResponseAsync(NetworkStream stream, Response response)
        {
            string json = JsonSerializer.Serialize(response, _jsonOptions);
            byte[] messageBytes = Encoding.UTF8.GetBytes(json);
            byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);

            await stream.WriteAsync(lengthBytes, 0, 4);
            await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
            await stream.FlushAsync();
        }

        private void Log(string message)
        {
            string timestamped = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Console.WriteLine(timestamped);
            OnLog?.Invoke(timestamped);
        }

        public void Stop()
        {
            _listener?.Stop();
            lock (_clientsLock)
            {
                foreach (TcpClient client in _connectedClients)
                    client.Close();
                _connectedClients.Clear();
            }
            Log("[SERVER] Stopped.");
        }
    }
}