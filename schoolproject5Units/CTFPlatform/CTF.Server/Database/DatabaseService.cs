using Microsoft.Data.Sqlite;
using CTF.Common.Models;
using CTF.Common.Services;

namespace CTF.Server.Database
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string dbPath = "ctf_platform.db")
        {
            _connectionString = $"Data Source={dbPath}";
            InitializeDatabase();
        }

        // creates the tables if not exists
        private void InitializeDatabase()
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();

            string sql = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    Role TEXT NOT NULL DEFAULT 'Player',
                    Score INTEGER NOT NULL DEFAULT 0,
                    RegisteredAt TEXT NOT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 1
                );

                CREATE TABLE IF NOT EXISTS Categories (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL UNIQUE,
                    Description TEXT
                );

                CREATE TABLE IF NOT EXISTS Challenges (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Description TEXT NOT NULL,
                    EncryptedFlag TEXT NOT NULL,
                    Points INTEGER NOT NULL,
                    Difficulty TEXT NOT NULL,
                    CategoryId INTEGER NOT NULL,
                    CreatorId INTEGER NOT NULL,
                    IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT NOT NULL,
                    FOREIGN KEY (CategoryId) REFERENCES Categories(Id),
                    FOREIGN KEY (CreatorId) REFERENCES Users(Id)
                );

                CREATE TABLE IF NOT EXISTS Hints (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ChallengeId INTEGER NOT NULL,
                    Content TEXT NOT NULL,
                    PointCost INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (ChallengeId) REFERENCES Challenges(Id)
                );

                CREATE TABLE IF NOT EXISTS ChallengeFiles (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ChallengeId INTEGER NOT NULL,
                    FileName TEXT NOT NULL,
                    FileType TEXT NOT NULL,
                    FileData BLOB NOT NULL,
                    FOREIGN KEY (ChallengeId) REFERENCES Challenges(Id)
                );

                CREATE TABLE IF NOT EXISTS Submissions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    ChallengeId INTEGER NOT NULL,
                    SubmittedFlag TEXT NOT NULL,
                    IsCorrect INTEGER NOT NULL,
                    SubmittedAt TEXT NOT NULL,
                    FOREIGN KEY (UserId) REFERENCES Users(Id),
                    FOREIGN KEY (ChallengeId) REFERENCES Challenges(Id)
                );

                CREATE TABLE IF NOT EXISTS HintUnlocks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    HintId INTEGER NOT NULL,
                    UnlockedAt TEXT NOT NULL,
                    FOREIGN KEY (UserId) REFERENCES Users(Id),
                    FOREIGN KEY (HintId) REFERENCES Hints(Id),
                    UNIQUE(UserId, HintId)
                );
            ";

            using SqliteCommand cmd = new(sql, conn);
            cmd.ExecuteNonQuery();

            SeedInitialData(conn);
        }

        // default 
        private void SeedInitialData(SqliteConnection conn)
        {
            // check if admin exists
            using SqliteCommand checkCmd = new("SELECT COUNT(*) FROM Users WHERE Role='Admin'", conn);
            long count = (long)(checkCmd.ExecuteScalar() ?? 0L);
            if (count > 0) return;

            // intialize admin 
            string adminHash = CryptoService.HashPassword("admin123");
            using SqliteCommand adminCmd = new(@"
                INSERT INTO Users (Username, PasswordHash, Role, RegisteredAt)
                VALUES (@u, @p, 'Admin', @d)", conn);
            adminCmd.Parameters.AddWithValue("@u", "admin");
            adminCmd.Parameters.AddWithValue("@p", adminHash);
            adminCmd.Parameters.AddWithValue("@d", DateTime.UtcNow.ToString("o"));
            adminCmd.ExecuteNonQuery();

            // default categories
            string[] categories = { "Crypto", "Web", "Forensics", "Reversing", "Pwn", "Misc", "Other" };
            foreach (string cat in categories)
            {
                using SqliteCommand catCmd = new(@"
                    INSERT OR IGNORE INTO Categories (Name, Description)
                    VALUES (@n, @d)", conn);
                catCmd.Parameters.AddWithValue("@n", cat);
                catCmd.Parameters.AddWithValue("@d", $"{cat} challenges");
                catCmd.ExecuteNonQuery();
            }
        }

        // ==================== USERS ====================

        public User? GetUserByUsername(string username)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new(@"
                SELECT Id, Username, PasswordHash, Role, Score, RegisteredAt, IsActive
                FROM Users WHERE Username = @u", conn);
            cmd.Parameters.AddWithValue("@u", username);

            using SqliteDataReader reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return MapUser(reader);
        }

        public User? GetUserById(int id)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new(@"
                SELECT Id, Username, PasswordHash, Role, Score, RegisteredAt, IsActive
                FROM Users WHERE Id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            using SqliteDataReader reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;
            return MapUser(reader);
        }

        public bool RegisterUser(string username, string passwordHash, string role = "Player")
        {
            try
            {
                using SqliteConnection conn = new(_connectionString);
                conn.Open();
                using SqliteCommand cmd = new(@"
                    INSERT INTO Users (Username, PasswordHash, Role, RegisteredAt)
                    VALUES (@u, @p, @r, @d)", conn);
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@p", passwordHash);
                cmd.Parameters.AddWithValue("@r", role);
                cmd.Parameters.AddWithValue("@d", DateTime.UtcNow.ToString("o"));
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException)
            {
                return false; // Username exists
            }
        }

        public void UpdateUserScore(int userId, int points)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new(@"
                UPDATE Users SET Score = Score + @p WHERE Id = @id", conn);
            cmd.Parameters.AddWithValue("@p", points);
            cmd.Parameters.AddWithValue("@id", userId);
            cmd.ExecuteNonQuery();
        }

        public List<User> GetAllUsers()
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new(@"
                SELECT Id, Username, PasswordHash, Role, Score, RegisteredAt, IsActive
                FROM Users ORDER BY Score DESC", conn);

            using SqliteDataReader reader = cmd.ExecuteReader();
            List<User> users = new();
            while (reader.Read())
                users.Add(MapUser(reader));
            return users;
        }

        // ==================== CHALLENGES ====================

        public List<Challenge> GetAllChallenges(bool activeOnly = true)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            string where = activeOnly ? "WHERE c.IsActive = 1" : "";
            using SqliteCommand cmd = new($@"
                SELECT c.Id, c.Title, c.Description, c.EncryptedFlag,
                       c.Points, c.Difficulty, c.CategoryId, cat.Name as CategoryName,
                       c.CreatorId, c.IsActive, c.CreatedAt
                FROM Challenges c
                JOIN Categories cat ON c.CategoryId = cat.Id
                {where}
                ORDER BY c.Points ASC", conn);

            using SqliteDataReader reader = cmd.ExecuteReader();
            List<Challenge> challenges = new();
            while (reader.Read())
                challenges.Add(MapChallenge(reader));
            return challenges;
        }

        public Challenge? GetChallengeById(int id)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new(@"
                SELECT c.Id, c.Title, c.Description, c.EncryptedFlag,
                       c.Points, c.Difficulty, c.CategoryId, cat.Name as CategoryName,
                       c.CreatorId, c.IsActive, c.CreatedAt
                FROM Challenges c
                JOIN Categories cat ON c.CategoryId = cat.Id
                WHERE c.Id = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            using SqliteDataReader reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;
            return MapChallenge(reader);
        }

        public bool UpdateChallenge(CTF.Common.Packets.UpdateChallengeRequest req)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            string sql = string.IsNullOrWhiteSpace(req.Flag)
                ? "UPDATE Challenges SET Title=@t, Description=@desc, Points=@pts, Difficulty=@diff, CategoryId=@cat, IsActive=@active WHERE Id=@id"
                : "UPDATE Challenges SET Title=@t, Description=@desc, EncryptedFlag=@flag, Points=@pts, Difficulty=@diff, CategoryId=@cat, IsActive=@active WHERE Id=@id";
            using SqliteCommand cmd = new(sql, conn);
            cmd.Parameters.AddWithValue("@t", req.Title);
            cmd.Parameters.AddWithValue("@desc", req.Description);
            cmd.Parameters.AddWithValue("@pts", req.Points);
            cmd.Parameters.AddWithValue("@diff", req.Difficulty);
            cmd.Parameters.AddWithValue("@cat", req.CategoryId);
            cmd.Parameters.AddWithValue("@active", req.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", req.Id);
            if (!string.IsNullOrWhiteSpace(req.Flag)) cmd.Parameters.AddWithValue("@flag", req.Flag);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool DeleteChallenge(int id)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteTransaction tx = conn.BeginTransaction();
            try
            {
                // del hints
                using SqliteCommand deleteHints = new(
                    "DELETE FROM Hints WHERE ChallengeId=@id", conn, tx);
                deleteHints.Parameters.AddWithValue("@id", id);
                deleteHints.ExecuteNonQuery();

                // del hint unlocks
                using SqliteCommand deleteUnlocks = new(@"
            DELETE FROM HintUnlocks WHERE HintId IN 
            (SELECT Id FROM Hints WHERE ChallengeId=@id)", conn, tx);
                deleteUnlocks.Parameters.AddWithValue("@id", id);
                deleteUnlocks.ExecuteNonQuery();

                // del files
                using SqliteCommand deleteFiles = new(
                    "DELETE FROM ChallengeFiles WHERE ChallengeId=@id", conn, tx);
                deleteFiles.Parameters.AddWithValue("@id", id);
                deleteFiles.ExecuteNonQuery();

                // del submissions
                using SqliteCommand deleteSubmissions = new(
                    "DELETE FROM Submissions WHERE ChallengeId=@id", conn, tx);
                deleteSubmissions.Parameters.AddWithValue("@id", id);
                deleteSubmissions.ExecuteNonQuery();

                // del challenge
                using SqliteCommand deleteChallenge = new(
                    "DELETE FROM Challenges WHERE Id=@id", conn, tx);
                deleteChallenge.Parameters.AddWithValue("@id", id);
                int rows = deleteChallenge.ExecuteNonQuery();

                tx.Commit();
                return rows > 0;
            }
            catch
            {
                tx.Rollback();
                return false;
            }
        }

        public int CreateChallenge(Challenge challenge)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new(@"
                INSERT INTO Challenges (Title, Description, EncryptedFlag, Points, Difficulty,
                                        CategoryId, CreatorId, IsActive, CreatedAt)
                VALUES (@t, @desc, @flag, @pts, @diff, @cat, @creator, 1, @date);
                SELECT last_insert_rowid();", conn);
            cmd.Parameters.AddWithValue("@t", challenge.Title);
            cmd.Parameters.AddWithValue("@desc", challenge.Description);
            cmd.Parameters.AddWithValue("@flag", challenge.EncryptedFlag);
            cmd.Parameters.AddWithValue("@pts", challenge.Points);
            cmd.Parameters.AddWithValue("@diff", challenge.Difficulty.ToString());
            cmd.Parameters.AddWithValue("@cat", challenge.CategoryId);
            cmd.Parameters.AddWithValue("@creator", challenge.CreatorId);
            cmd.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("o"));

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // ==================== SUBMISSIONS ====================

        public bool HasAlreadySolved(int userId, int challengeId)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new(@"
                SELECT COUNT(*) FROM Submissions
                WHERE UserId = @u AND ChallengeId = @c AND IsCorrect = 1", conn);
            cmd.Parameters.AddWithValue("@u", userId);
            cmd.Parameters.AddWithValue("@c", challengeId);
            return (long)(cmd.ExecuteScalar() ?? 0L) > 0;
        }

        public void AddSubmission(Submission submission)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new(@"
                INSERT INTO Submissions (UserId, ChallengeId, SubmittedFlag, IsCorrect, SubmittedAt)
                VALUES (@u, @c, @flag, @correct, @date)", conn);
            cmd.Parameters.AddWithValue("@u", submission.UserId);
            cmd.Parameters.AddWithValue("@c", submission.ChallengeId);
            cmd.Parameters.AddWithValue("@flag", submission.SubmittedFlag);
            cmd.Parameters.AddWithValue("@correct", submission.IsCorrect ? 1 : 0);
            cmd.Parameters.AddWithValue("@date", DateTime.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        public bool ChangeUserRole(int userId, string newRole)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new(@"
                UPDATE Users SET Role = @role WHERE Id = @id AND Role != 'Admin'", conn);
            cmd.Parameters.AddWithValue("@role", newRole);
            cmd.Parameters.AddWithValue("@id", userId);
            return cmd.ExecuteNonQuery() > 0;
        }

        // ==================== SCOREBOARD ====================

        public List<ScoreboardEntry> GetScoreboard()
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new(@"
                SELECT u.Username, u.Score,
                       COUNT(DISTINCT s.ChallengeId) as SolvedCount,
                       MAX(s.SubmittedAt) as LastSolve
                FROM Users u
                LEFT JOIN Submissions s ON u.Id = s.UserId AND s.IsCorrect = 1
                WHERE u.Role = 'Player' AND u.IsActive = 1
                GROUP BY u.Id, u.Username, u.Score
                ORDER BY u.Score DESC, LastSolve ASC", conn);

            using SqliteDataReader reader = cmd.ExecuteReader();
            List<ScoreboardEntry> entries = new();
            int rank = 1;
            while (reader.Read())
            {
                entries.Add(new ScoreboardEntry
                {
                    Rank = rank++,
                    Username = reader.GetString(0),
                    Score = reader.GetInt32(1),
                    SolvedCount = reader.GetInt32(2),
                    LastSolve = reader.IsDBNull(3)
                        ? DateTime.MinValue
                        : DateTime.Parse(reader.GetString(3))
                });
            }
            return entries;
        }

        // ==================== HINTS ====================
        public int GetHintCost(int hintId)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new(
                "SELECT PointCost FROM Hints WHERE Id=@id", conn);
            cmd.Parameters.AddWithValue("@id", hintId);
            return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }
        public List<Hint> GetHintsForChallenge(int challengeId, int userId)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new(@"
                SELECT h.Id, h.ChallengeId, h.Content, h.PointCost,
                       CASE WHEN hu.Id IS NOT NULL THEN 1 ELSE 0 END as IsUnlocked
                FROM Hints h
                LEFT JOIN HintUnlocks hu ON h.Id = hu.HintId AND hu.UserId = @u
                WHERE h.ChallengeId = @c", conn);
            cmd.Parameters.AddWithValue("@u", userId);
            cmd.Parameters.AddWithValue("@c", challengeId);

            using SqliteDataReader reader = cmd.ExecuteReader();
            List<Hint> hints = new();
            while (reader.Read())
            {
                bool unlocked = reader.GetInt32(4) == 1;
                hints.Add(new Hint
                {
                    Id = reader.GetInt32(0),
                    ChallengeId = reader.GetInt32(1),
                    Content = unlocked ? reader.GetString(2) : "🔒 Locked",
                    PointCost = reader.GetInt32(3),
                    IsUnlocked = unlocked
                });
            }
            return hints;
        }

        public bool UnlockHint(int userId, int hintId, int pointCost)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteTransaction tx = conn.BeginTransaction();
            try
            {
                using SqliteCommand insertCmd = new(@"
                    INSERT OR IGNORE INTO HintUnlocks (UserId, HintId, UnlockedAt)
                    VALUES (@u, @h, @d)", conn, tx);
                insertCmd.Parameters.AddWithValue("@u", userId);
                insertCmd.Parameters.AddWithValue("@h", hintId);
                insertCmd.Parameters.AddWithValue("@d", DateTime.UtcNow.ToString("o"));
                int rows = insertCmd.ExecuteNonQuery();

                if (rows > 0 && pointCost > 0)
                {
                    using SqliteCommand scoreCmd = new(@"
                        UPDATE Users SET Score = Score - @cost WHERE Id = @u AND Score >= @cost", conn, tx);
                    scoreCmd.Parameters.AddWithValue("@cost", pointCost);
                    scoreCmd.Parameters.AddWithValue("@u", userId);
                    scoreCmd.ExecuteNonQuery();
                }

                tx.Commit();
                return rows > 0;
            }
            catch
            {
                tx.Rollback();
                return false;
            }
        }
        public void AddHint(int challengeId, string content, int pointCost)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new(@"
        INSERT INTO Hints (ChallengeId, Content, PointCost)
        VALUES (@c, @content, @cost)", conn);
            cmd.Parameters.AddWithValue("@c", challengeId);
            cmd.Parameters.AddWithValue("@content", content);
            cmd.Parameters.AddWithValue("@cost", pointCost);
            cmd.ExecuteNonQuery();
        }

        public void DeleteHint(int hintId)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new("DELETE FROM Hints WHERE Id=@id", conn);
            cmd.Parameters.AddWithValue("@id", hintId);
            cmd.ExecuteNonQuery();
        }

        public List<Hint> GetHintsByChallengeId(int challengeId)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new(@"
        SELECT Id, ChallengeId, Content, PointCost
        FROM Hints WHERE ChallengeId=@c", conn);
            cmd.Parameters.AddWithValue("@c", challengeId);
            using SqliteDataReader reader = cmd.ExecuteReader();
            List<Hint> hints = new();
            while (reader.Read())
                hints.Add(new Hint
                {
                    Id = reader.GetInt32(0),
                    ChallengeId = reader.GetInt32(1),
                    Content = reader.GetString(2),
                    PointCost = reader.GetInt32(3)
                });
            return hints;
        }

        // ==================== FILES ====================

        public ChallengeFile? GetFile(int fileId)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new(@"
                SELECT Id, ChallengeId, FileName, FileType, FileData
                FROM ChallengeFiles WHERE Id = @id", conn);
            cmd.Parameters.AddWithValue("@id", fileId);

            using SqliteDataReader reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new ChallengeFile
            {
                Id = reader.GetInt32(0),
                ChallengeId = reader.GetInt32(1),
                FileName = reader.GetString(2),
                FileType = reader.GetString(3),
                FileData = (byte[])reader["FileData"]
            };
        }
        public bool DeleteFile(int fileId)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new("DELETE FROM ChallengeFiles WHERE Id=@id", conn);
            cmd.Parameters.AddWithValue("@id", fileId);
            return cmd.ExecuteNonQuery() > 0;
        }
        public void UploadFile(int challengeId, string fileName, string fileType, byte[] fileData)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new(@"
        INSERT INTO ChallengeFiles (ChallengeId, FileName, FileType, FileData)
        VALUES (@c, @name, @type, @data)", conn);
            cmd.Parameters.AddWithValue("@c", challengeId);
            cmd.Parameters.AddWithValue("@name", fileName);
            cmd.Parameters.AddWithValue("@type", fileType);
            cmd.Parameters.AddWithValue("@data", fileData);
            cmd.ExecuteNonQuery();
        }

        public List<ChallengeFile> GetFilesByChallenge(int challengeId)
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new(@"
        SELECT Id, ChallengeId, FileName, FileType
        FROM ChallengeFiles WHERE ChallengeId=@c", conn);
            cmd.Parameters.AddWithValue("@c", challengeId);
            using SqliteDataReader reader = cmd.ExecuteReader();
            List<ChallengeFile> files = new();
            while (reader.Read())
                files.Add(new ChallengeFile
                {
                    Id = reader.GetInt32(0),
                    ChallengeId = reader.GetInt32(1),
                    FileName = reader.GetString(2),
                    FileType = reader.GetString(3)
                });
            return files;
        }
        public List<Category> GetCategories()
        {
            using SqliteConnection conn = new(_connectionString);
            conn.Open();
            using SqliteCommand cmd = new("SELECT Id, Name, Description FROM Categories", conn);
            using SqliteDataReader reader = cmd.ExecuteReader();
            List<Category> categories = new();
            while (reader.Read())
                categories.Add(new Category
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2)
                });
            return categories;
        }

        // ==================== MAPPERS ====================

        private static User MapUser(SqliteDataReader r) => new()
        {
            Id = r.GetInt32(0),
            Username = r.GetString(1),
            PasswordHash = r.GetString(2),
            Role = Enum.Parse<UserRole>(r.GetString(3)),
            Score = r.GetInt32(4),
            RegisteredAt = DateTime.Parse(r.GetString(5)),
            IsActive = r.GetInt32(6) == 1
        };

        private static Challenge MapChallenge(SqliteDataReader r) => new()
        {
            Id = r.GetInt32(0),
            Title = r.GetString(1),
            Description = r.GetString(2),
            EncryptedFlag = r.GetString(3),
            Points = r.GetInt32(4),
            Difficulty = Enum.Parse<DifficultyLevel>(r.GetString(5)),
            CategoryId = r.GetInt32(6),
            CategoryName = r.GetString(7),
            CreatorId = r.GetInt32(8),
            IsActive = r.GetInt32(9) == 1,
            CreatedAt = DateTime.Parse(r.GetString(10))
        };
    }
}