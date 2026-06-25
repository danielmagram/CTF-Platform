using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using CTF.Common.Models;
using CTF.Common.Packets;

namespace CTF.Client
{
    public class ServerConnection
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly SemaphoreSlim _streamLock = new(1, 1);

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            MaxDepth = 64
        };

        public string Token { get; private set; } = "";
        public User? CurrentUser { get; private set; }
        public bool IsConnected => _client?.Connected ?? false;

        public delegate void ServerBroadcastHandler(string message);
        public event ServerBroadcastHandler? OnBroadcastReceived;

        public ServerConnection(string host = "127.0.0.1", int port = 5000)
        {
            _host = host;
            _port = port;
        }

        public async Task ConnectAsync()
        {
            _client = new TcpClient();
            await _client.ConnectAsync(_host, _port);
            _stream = _client.GetStream();
        }

        public void Disconnect()
        {
            if (IsConnected)
                SendPacket(new Packet { Type = PacketType.Logout, Token = Token });
            _stream?.Close();
            _client?.Close();
        }

        // ==================== AUTH ====================

        public async Task<(bool success, string message, LoginResponse? data)> LoginAsync(
            string username, string password)
        {
            Packet packet = new()
            {
                Type = PacketType.Login,
                Payload = JsonSerializer.Serialize(new LoginRequest
                {
                    Username = username,
                    Password = password
                }, _jsonOptions)
            };

            Response response = await SendAndReceiveAsync(packet);
            if (!response.Success) return (false, response.Message, null);

            LoginResponse? loginData = JsonSerializer.Deserialize<LoginResponse>(response.Data, _jsonOptions);
            if (loginData != null)
            {
                Token = loginData.Token;
                CurrentUser = new User
                {
                    Id = loginData.UserId,
                    Username = loginData.Username,
                    Role = Enum.Parse<UserRole>(loginData.Role),
                    Score = loginData.Score
                };
            }

            return (true, "Login successful", loginData);
        }

        public async Task<(bool success, string message)> RegisterAsync(
            string username, string password)
        {
            Response response = await SendAndReceiveAsync(new Packet
            {
                Type = PacketType.Register,
                Payload = JsonSerializer.Serialize(new LoginRequest
                {
                    Username = username,
                    Password = password
                }, _jsonOptions)
            });
            return (response.Success, response.Message);
        }

        // ==================== CHALLENGES ====================

        public async Task<List<Challenge>> GetChallengesAsync()
        {
            Response response = await SendAndReceiveAsync(new Packet
            {
                Type = PacketType.GetChallenges,
                Token = Token
            });
            if (!response.Success) return new();
            return JsonSerializer.Deserialize<List<Challenge>>(response.Data, _jsonOptions) ?? new();
        }

        public async Task<(bool success, SubmitFlagResponse? data, string message)> SubmitFlagAsync(
            int challengeId, string flag)
        {
            Response response = await SendAndReceiveAsync(new Packet
            {
                Type = PacketType.SubmitFlag,
                Token = Token,
                Payload = JsonSerializer.Serialize(new SubmitFlagRequest
                {
                    ChallengeId = challengeId,
                    Flag = flag
                }, _jsonOptions)
            });
            if (!response.Success) return (false, null, response.Message);
            SubmitFlagResponse? data = JsonSerializer.Deserialize<SubmitFlagResponse>(response.Data, _jsonOptions);
            return (true, data, response.Message);
        }

        // ==================== SCOREBOARD ====================

        public async Task<List<ScoreboardEntry>> GetScoreboardAsync()
        {
            Response response = await SendAndReceiveAsync(new Packet
            {
                Type = PacketType.GetScoreboard,
                Token = Token
            });
            if (!response.Success) return new();
            return JsonSerializer.Deserialize<List<ScoreboardEntry>>(response.Data, _jsonOptions) ?? new();
        }

        // ==================== HINTS ====================

        public async Task<List<Hint>> GetHintsAsync(int challengeId)
        {
            Response response = await SendAndReceiveAsync(new Packet
            {
                Type = PacketType.GetHints,
                Token = Token,
                Payload = challengeId.ToString()
            });
            if (!response.Success) return new();
            return JsonSerializer.Deserialize<List<Hint>>(response.Data, _jsonOptions) ?? new();
        }

        public async Task<List<Hint>> GetHintsByChallengeAsync(int challengeId)
        {
            Response response = await SendAndReceiveAsync(new Packet
            {
                Type = PacketType.GetHintsByChallenge,
                Token = Token,
                Payload = challengeId.ToString()
            });
            if (!response.Success) return new();
            return JsonSerializer.Deserialize<List<Hint>>(response.Data, _jsonOptions) ?? new();
        }

        public async Task<(bool success, string message, string content, int newScore)> UnlockHintAsync(
            int hintId, int challengeId)
        {
            Response response = await SendAndReceiveAsync(new Packet
            {
                Type = PacketType.UnlockHint,
                Token = Token,
                Payload = JsonSerializer.Serialize(new UnlockHintRequest
                {
                    HintId = hintId,
                    ChallengeId = challengeId
                }, _jsonOptions)
            });

            if (!response.Success) return (false, response.Message, "", 0);
            var data = JsonSerializer.Deserialize<JsonElement>(response.Data, _jsonOptions);
            string content = data.GetProperty("Content").GetString() ?? "";
            int newScore = data.GetProperty("NewScore").GetInt32();
            return (true, response.Message, content, newScore);
        }

        public async Task<(bool success, string message)> AddHintAsync(
            int challengeId, string content, int pointCost)
        {
            Response response = await SendAndReceiveAsync(new Packet
            {
                Type = PacketType.AddHint,
                Token = Token,
                Payload = JsonSerializer.Serialize(new AddHintRequest
                {
                    ChallengeId = challengeId,
                    Content = content,
                    PointCost = pointCost
                }, _jsonOptions)
            });
            return (response.Success, response.Message);
        }

        public async Task<(bool success, string message)> DeleteHintAsync(int hintId)
        {
            Response response = await SendAndReceiveAsync(new Packet
            {
                Type = PacketType.DeleteHint,
                Token = Token,
                Payload = JsonSerializer.Serialize(new DeleteHintRequest { HintId = hintId }, _jsonOptions)
            });
            return (response.Success, response.Message);
        }

        // ==================== FILES ====================

        public async Task<DownloadFileResponse?> DownloadFileAsync(int fileId)
        {
            Response response = await SendAndReceiveAsync(new Packet
            {
                Type = PacketType.DownloadFile,
                Token = Token,
                Payload = JsonSerializer.Serialize(new DownloadFileRequest { FileId = fileId }, _jsonOptions)
            });
            if (!response.Success) return null;
            return JsonSerializer.Deserialize<DownloadFileResponse>(response.Data, _jsonOptions);
        }
        public async Task<(bool success, string message)> DeleteFileAsync(int fileId)
        {
            Response response = await SendAndReceiveAsync(new Packet
            {
                Type = PacketType.DeleteFile,
                Token = Token,
                Payload = JsonSerializer.Serialize(new DeleteFileRequest { FileId = fileId }, _jsonOptions)
            });
            return (response.Success, response.Message);
        }
        public async Task<(bool success, string message)> UploadFileAsync(
            int challengeId, string fileName, byte[] fileData)
        {
            string fileType = Path.GetExtension(fileName).ToLower();
            Response response = await SendAndReceiveAsync(new Packet
            {
                Type = PacketType.UploadFile,
                Token = Token,
                Payload = JsonSerializer.Serialize(new UploadFileRequest
                {
                    ChallengeId = challengeId,
                    FileName = fileName,
                    FileType = fileType,
                    FileData = fileData
                }, _jsonOptions)
            });
            return (response.Success, response.Message);
        }

        public async Task<List<ChallengeFile>> GetChallengeFilesAsync(int challengeId)
        {
            Response response = await SendAndReceiveAsync(new Packet
            {
                Type = PacketType.GetChallengeFiles,
                Token = Token,
                Payload = challengeId.ToString()
            });
            if (!response.Success) return new();
            return JsonSerializer.Deserialize<List<ChallengeFile>>(response.Data, _jsonOptions) ?? new();
        }

        // ==================== CREATOR / ADMIN ====================

        public async Task<(bool success, string message)> CreateChallengeAsync(CreateChallengeRequest req)
        {
            Response response = await SendAndReceiveAsync(new Packet
            {
                Type = PacketType.CreateChallenge,
                Token = Token,
                Payload = JsonSerializer.Serialize(req, _jsonOptions)
            });
            return (response.Success, response.Data); // Data contains the id
        }

        public async Task<(bool success, string message)> UpdateChallengeAsync(UpdateChallengeRequest req)
        {
            Response response = await SendAndReceiveAsync(new Packet
            {
                Type = PacketType.UpdateChallenge,
                Token = Token,
                Payload = JsonSerializer.Serialize(req, _jsonOptions)
            });
            return (response.Success, response.Message);
        }

        public async Task<(bool success, string message)> DeleteChallengeAsync(int id)
        {
            Response response = await SendAndReceiveAsync(new Packet
            {
                Type = PacketType.DeleteChallenge,
                Token = Token,
                Payload = id.ToString()
            });
            return (response.Success, response.Message);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            Response response = await SendAndReceiveAsync(new Packet
            {
                Type = PacketType.GetAllUsers,
                Token = Token
            });
            if (!response.Success) return new();
            return JsonSerializer.Deserialize<List<User>>(response.Data, _jsonOptions) ?? new();
        }

        public async Task<(bool success, string message)> ChangeUserRoleAsync(int userId, string newRole)
        {
            Response response = await SendAndReceiveAsync(new Packet
            {
                Type = PacketType.ChangeUserRole,
                Token = Token,
                Payload = JsonSerializer.Serialize(new ChangeUserRoleRequest
                {
                    UserId = userId,
                    NewRole = newRole
                }, _jsonOptions)
            });
            return (response.Success, response.Message);
        }

        // ==================== TCP HELPERS ====================

        private async Task<Response> SendAndReceiveAsync(Packet packet)
        {
            if (_stream == null)
                return new Response { Success = false, Message = "Not connected" };

            await _streamLock.WaitAsync();
            try
            {
                await SendPacketAsync(packet);
                return await ReceiveResponseAsync();
            }
            finally
            {
                _streamLock.Release();
            }
        }

        private async Task SendPacketAsync(Packet packet)
        {
            string json = JsonSerializer.Serialize(packet, _jsonOptions);
            byte[] messageBytes = Encoding.UTF8.GetBytes(json);
            byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);

            await _stream!.WriteAsync(lengthBytes, 0, 4);
            await _stream!.WriteAsync(messageBytes, 0, messageBytes.Length);
            await _stream!.FlushAsync();
        }

        private void SendPacket(Packet packet)
        {
            try { SendPacketAsync(packet).Wait(); } catch { }
        }

        private async Task<Response> ReceiveResponseAsync()
        {
            try
            {
                byte[] lengthBytes = new byte[4];
                int read = await _stream!.ReadAsync(lengthBytes, 0, 4);
                if (read == 0)
                    return new Response { Success = false, Message = "Connection closed" };

                int length = BitConverter.ToInt32(lengthBytes, 0);

                byte[] messageBytes = new byte[length];
                int totalRead = 0;
                while (totalRead < length)
                {
                    int r = await _stream.ReadAsync(messageBytes, totalRead, length - totalRead);
                    if (r == 0) break;
                    totalRead += r;
                }

                string json = Encoding.UTF8.GetString(messageBytes, 0, totalRead);
                return JsonSerializer.Deserialize<Response>(json, _jsonOptions)
                       ?? new Response { Success = false, Message = "Invalid response" };
            }
            catch (Exception ex)
            {
                return new Response { Success = false, Message = ex.Message };
            }
        }
    }
}