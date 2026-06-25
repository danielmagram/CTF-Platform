using System.Text.Json;
using CTF.Common.Models;
using CTF.Common.Packets;
using CTF.Common.Services;
using CTF.Server.Database;

namespace CTF.Server
{
    public class RequestHandler
    {
        private readonly DatabaseService _db;
        private readonly SessionManager _sessions;

        // Delegate + Event — triggered when score changes (updates all clients)
        public delegate void ScoreChangedHandler(string username, int newScore);
        public event ScoreChangedHandler? OnScoreChanged;

        // Delegate + Event — triggered when a new challenge is created
        public delegate void ChallengeCreatedHandler(string challengeTitle);
        public event ChallengeCreatedHandler? OnChallengeCreated;

        public RequestHandler(DatabaseService db, SessionManager sessions)
        {
            _db = db;
            _sessions = sessions;
        }

        public Response Handle(Packet packet)
        {
            return packet.Type switch
            {
                PacketType.Login => HandleLogin(packet),
                PacketType.Register => HandleRegister(packet),
                PacketType.Logout => HandleLogout(packet),
                PacketType.GetChallenges => HandleGetChallenges(packet),
                PacketType.SubmitFlag => HandleSubmitFlag(packet),
                PacketType.GetScoreboard => HandleGetScoreboard(packet),
                PacketType.GetHints => HandleGetHints(packet),
                PacketType.AddHint => HandleAddHint(packet),
                PacketType.DeleteHint => HandleDeleteHint(packet),
                PacketType.GetHintsByChallenge => HandleGetHintsByChallenge(packet),
                PacketType.UnlockHint => HandleUnlockHint(packet),
                PacketType.DownloadFile => HandleDownloadFile(packet),
                PacketType.UploadFile => HandleUploadFile(packet),
                PacketType.DeleteFile => HandleDeleteFile(packet),
                PacketType.GetChallengeFiles => HandleGetChallengeFiles(packet),
                PacketType.CreateChallenge => HandleCreateChallenge(packet),
                PacketType.UpdateChallenge => HandleUpdateChallenge(packet),
                PacketType.DeleteChallenge => HandleDeleteChallenge(packet),
                PacketType.GetAllUsers => HandleGetAllUsers(packet),
                PacketType.ChangeUserRole => HandleChangeUserRole(packet),
                _ => Error("Unknown request type")
            };
        }

        // ==================== AUTH ====================

        private Response HandleLogin(Packet packet)
        {
            LoginRequest? req = Deserialize<LoginRequest>(packet.Payload);
            if (req == null) return Error("Invalid request");

            // Validation
            var usernameCheck = ValidationService.ValidateUsername(req.Username);
            if (!usernameCheck.IsValid) return Error(usernameCheck.Message);

            User? user = _db.GetUserByUsername(req.Username);
            if (user == null) return Error("User not found");
            if (!user.IsActive) return Error("Account is disabled");
            if (!CryptoService.VerifyPassword(req.Password, user.PasswordHash))
                return Error("Incorrect password");

            string token = _sessions.CreateSession(user);

            return Success(new LoginResponse
            {
                UserId = user.Id,
                Username = user.Username,
                Role = user.Role.ToString(),
                Token = token,
                Score = user.Score
            });
        }

        private Response HandleRegister(Packet packet)
        {
            LoginRequest? req = Deserialize<LoginRequest>(packet.Payload);
            if (req == null) return Error("Invalid request");

            var usernameCheck = ValidationService.ValidateUsername(req.Username);
            if (!usernameCheck.IsValid) return Error(usernameCheck.Message);

            var passwordCheck = ValidationService.ValidatePassword(req.Password);
            if (!passwordCheck.IsValid) return Error(passwordCheck.Message);

            string hash = CryptoService.HashPassword(req.Password);
            bool created = _db.RegisterUser(req.Username, hash);

            return created ? Success("Registered successfully") : Error("Username already taken");
        }

        private Response HandleLogout(Packet packet)
        {
            _sessions.RemoveSession(packet.Token);
            return Success("Logged out");
        }

        // ==================== CHALLENGES ====================

        private Response HandleGetChallenges(Packet packet)
        {
            if (!_sessions.IsLoggedIn(packet.Token)) return Error("Not authenticated");

            List<Challenge> challenges = _db.GetAllChallenges();

            // not sending the encrypted flag
            foreach (Challenge c in challenges)
                c.EncryptedFlag = "";

            return Success(challenges);
        }

        private Response HandleSubmitFlag(Packet packet)
        {
            User? user = _sessions.GetUser(packet.Token);
            if (user == null) return Error("Not authenticated");

            SubmitFlagRequest? req = Deserialize<SubmitFlagRequest>(packet.Payload);
            if (req == null) return Error("Invalid request");

            // Validation
            var flagCheck = ValidationService.ValidateFlag(req.Flag);
            if (!flagCheck.IsValid) return Error(flagCheck.Message);

            // check if solved
            if (_db.HasAlreadySolved(user.Id, req.ChallengeId))
                return Error("You already solved this challenge!");

            Challenge? challenge = _db.GetChallengeById(req.ChallengeId);
            if (challenge == null) return Error("Challenge not found");

            string correctFlag = CryptoService.DecryptFlag(challenge.EncryptedFlag);
            bool isCorrect = req.Flag.Trim() == correctFlag.Trim();

            _db.AddSubmission(new Submission
            {
                UserId = user.Id,
                ChallengeId = req.ChallengeId,
                SubmittedFlag = req.Flag,
                IsCorrect = isCorrect
            });

            int pointsEarned = 0;
            int newScore = user.Score;

            if (isCorrect)
            {
                pointsEarned = challenge.Points;
                _db.UpdateUserScore(user.Id, pointsEarned);
                newScore = user.Score + pointsEarned;

                OnScoreChanged?.Invoke(user.Username, newScore);
            }

            return Success(new SubmitFlagResponse
            {
                IsCorrect = isCorrect,
                PointsEarned = pointsEarned,
                NewScore = newScore,
                Message = isCorrect ? "🎉 Correct! Well done!" : "❌ Wrong flag, try again."
            });
        }

        // ==================== SCOREBOARD ====================

        private Response HandleGetScoreboard(Packet packet)
        {
            if (!_sessions.IsLoggedIn(packet.Token)) return Error("Not authenticated");
            return Success(_db.GetScoreboard());
        }

        // ==================== HINTS ====================

        private Response HandleGetHints(Packet packet)
        {
            User? user = _sessions.GetUser(packet.Token);
            if (user == null) return Error("Not authenticated");

            if (!int.TryParse(packet.Payload, out int challengeId))
                return Error("Invalid challenge id");

            return Success(_db.GetHintsForChallenge(challengeId, user.Id));
        }

        private Response HandleUnlockHint(Packet packet)
        {
            User? user = _sessions.GetUser(packet.Token);
            if (user == null) return Error("Not authenticated");

            UnlockHintRequest? req = Deserialize<UnlockHintRequest>(packet.Payload);
            if (req == null) return Error("Invalid request");

            int pointCost = _db.GetHintCost(req.HintId);

            User? freshUser = _db.GetUserById(user.Id);
            if (freshUser == null) return Error("User not found");
            if (freshUser.Score < pointCost)
                return Error($"Not enough points! Need {pointCost} pts");

            bool unlocked = _db.UnlockHint(user.Id, req.HintId, pointCost);
            if (!unlocked) return Error("Already unlocked");

            var hints = _db.GetHintsForChallenge(req.ChallengeId, user.Id);
            var hint = hints.FirstOrDefault(h => h.Id == req.HintId);

            return Success(new
            {
                Content = hint?.Content ?? "",
                PointCost = pointCost,
                NewScore = freshUser.Score - pointCost
            });
        }
        private Response HandleAddHint(Packet packet)
        {
            User? user = _sessions.GetUser(packet.Token);
            if (user == null) return Error("Not authenticated");
            if (user.Role == UserRole.Player) return Error("Permission denied");

            AddHintRequest? req = Deserialize<AddHintRequest>(packet.Payload);
            if (req == null) return Error("Invalid request");

            _db.AddHint(req.ChallengeId, req.Content, req.PointCost);
            return Success("Hint added!");
        }

        private Response HandleDeleteHint(Packet packet)
        {
            User? user = _sessions.GetUser(packet.Token);
            if (user == null) return Error("Not authenticated");
            if (user.Role == UserRole.Player) return Error("Permission denied");

            DeleteHintRequest? req = Deserialize<DeleteHintRequest>(packet.Payload);
            if (req == null) return Error("Invalid request");

            _db.DeleteHint(req.HintId);
            return Success("Hint deleted!");
        }

        private Response HandleGetHintsByChallenge(Packet packet)
        {
            User? user = _sessions.GetUser(packet.Token);
            if (user == null) return Error("Not authenticated");
            if (user.Role == UserRole.Player) return Error("Permission denied");

            if (!int.TryParse(packet.Payload, out int challengeId))
                return Error("Invalid challenge id");

            return Success(_db.GetHintsByChallengeId(challengeId));
        }

        // ==================== FILES ====================

        private Response HandleDownloadFile(Packet packet)
        {
            if (!_sessions.IsLoggedIn(packet.Token)) return Error("Not authenticated");

            DownloadFileRequest? req = Deserialize<DownloadFileRequest>(packet.Payload);
            if (req == null) return Error("Invalid request");

            ChallengeFile? file = _db.GetFile(req.FileId);
            if (file == null) return Error("File not found");

            return Success(new DownloadFileResponse
            {
                FileName = file.FileName,
                FileType = file.FileType,
                FileData = file.FileData
            });
        }
        private Response HandleDeleteFile(Packet packet)
        {
            User? user = _sessions.GetUser(packet.Token);
            if (user == null) return Error("Not authenticated");
            if (user.Role == UserRole.Player) return Error("Permission denied");

            DeleteFileRequest? req = Deserialize<DeleteFileRequest>(packet.Payload);
            if (req == null) return Error("Invalid request");

            bool deleted = _db.DeleteFile(req.FileId);
            return deleted ? Success("File deleted!") : Error("File not found");
        }

        private Response HandleUploadFile(Packet packet)
        {
            User? user = _sessions.GetUser(packet.Token);
            if (user == null) return Error("Not authenticated");
            if (user.Role == UserRole.Player) return Error("Permission denied");

            UploadFileRequest? req = Deserialize<UploadFileRequest>(packet.Payload);
            if (req == null) return Error("Invalid request");

            var fileCheck = ValidationService.ValidateFileType(req.FileName);
            if (!fileCheck.IsValid) return Error(fileCheck.Message);

            _db.UploadFile(req.ChallengeId, req.FileName, req.FileType, req.FileData);
            return Success("File uploaded!");
        }

        private Response HandleGetChallengeFiles(Packet packet)
        {
            if (!_sessions.IsLoggedIn(packet.Token)) return Error("Not authenticated");

            if (!int.TryParse(packet.Payload, out int challengeId))
                return Error("Invalid challenge id");

            return Success(_db.GetFilesByChallenge(challengeId));
        }
        // ==================== CREATOR / ADMIN ====================

        private Response HandleCreateChallenge(Packet packet)
        {
            User? user = _sessions.GetUser(packet.Token);
            if (user == null) return Error("Not authenticated");
            if (user.Role != UserRole.Creator && user.Role != UserRole.Admin)
                return Error("Permission denied");

            CreateChallengeRequest? req = Deserialize<CreateChallengeRequest>(packet.Payload);
            if (req == null) return Error("Invalid request");

            // Validation
            var titleCheck = ValidationService.ValidateChallengeTitle(req.Title);
            if (!titleCheck.IsValid) return Error(titleCheck.Message);

            var flagCheck = ValidationService.ValidateFlag(req.Flag);
            if (!flagCheck.IsValid) return Error(flagCheck.Message);

            Challenge challenge = new()
            {
                Title = req.Title,
                Description = req.Description,
                EncryptedFlag = CryptoService.EncryptFlag(req.Flag),
                Points = req.Points,
                Difficulty = Enum.Parse<DifficultyLevel>(req.Difficulty),
                CategoryId = req.CategoryId,
                CreatorId = user.Id
            };

            int id = _db.CreateChallenge(challenge);

            OnChallengeCreated?.Invoke(req.Title);

            return Success(id);
        }

        private Response HandleUpdateChallenge(Packet packet)
        {
            User? user = _sessions.GetUser(packet.Token);
            if (user == null) return Error("Not authenticated");
            if (user.Role != UserRole.Creator && user.Role != UserRole.Admin)
                return Error("Permission denied");

            UpdateChallengeRequest? req = Deserialize<UpdateChallengeRequest>(packet.Payload);
            if (req == null) return Error("Invalid request");

            var titleCheck = ValidationService.ValidateChallengeTitle(req.Title);
            if (!titleCheck.IsValid) return Error(titleCheck.Message);

            if (!string.IsNullOrWhiteSpace(req.Flag))
                req.Flag = CryptoService.EncryptFlag(req.Flag);

            bool updated = _db.UpdateChallenge(req);
            return updated ? Success("Challenge updated!") : Error("Challenge not found");
        }

        private Response HandleDeleteChallenge(Packet packet)
        {
            User? user = _sessions.GetUser(packet.Token);
            if (user == null) return Error("Not authenticated");
            if (user.Role != UserRole.Admin) return Error("Permission denied");

            if (!int.TryParse(packet.Payload, out int id))
                return Error("Invalid challenge id");

            bool deleted = _db.DeleteChallenge(id);
            return deleted ? Success("Challenge deleted!") : Error("Challenge not found");
        }

        private Response HandleChangeUserRole(Packet packet)
        {
            User? user = _sessions.GetUser(packet.Token);
            if (user == null) return Error("Not authenticated");
            if (user.Role != UserRole.Admin) return Error("Permission denied");

            ChangeUserRoleRequest? req = Deserialize<ChangeUserRoleRequest>(packet.Payload);
            if (req == null) return Error("Invalid request");

            if (req.NewRole != "Player" && req.NewRole != "Creator")
                return Error("Invalid role");

            bool changed = _db.ChangeUserRole(req.UserId, req.NewRole);
            return changed ? Success($"Role changed to {req.NewRole}") : Error("Could not change role");
        }

        private Response HandleGetAllUsers(Packet packet)
        {
            User? user = _sessions.GetUser(packet.Token);
            if (user == null) return Error("Not authenticated");
            if (user.Role != UserRole.Admin) return Error("Permission denied");

            return Success(_db.GetAllUsers());
        }

        // ==================== HELPERS ====================

        private static T? Deserialize<T>(string json)
        {
            try { return JsonSerializer.Deserialize<T>(json); }
            catch { return default; }
        }

        private static Response Success(object? data = null) => new()
        {
            Success = true,
            Message = "OK",
            Data = data == null ? "" : JsonSerializer.Serialize(data)
        };

        private static Response Success(string message) => new()
        {
            Success = true,
            Message = message,
            Data = ""
        };

        private static Response Error(string message) => new()
        {
            Success = false,
            Message = message,
            Data = ""
        };
    }
}