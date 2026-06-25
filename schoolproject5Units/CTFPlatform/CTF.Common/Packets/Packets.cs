namespace CTF.Common.Packets
{
    // Types of requests from client to server
    public enum PacketType
    {
        // Auth
        Login,
        Register,
        Logout,

        // Challenges
        GetChallenges,
        GetChallengeById,
        SubmitFlag,
        GetScoreboard,

        // Hints
        GetHints,
        UnlockHint,
        AddHint,
        DeleteHint,
        GetHintsByChallenge,

        // Files
        DownloadFile,
        UploadFile,
        GetChallengeFiles,
        DeleteFile,

        // Creator / Admin
        CreateChallenge,
        UpdateChallenge,
        DeleteChallenge,
        GetAllUsers,
        SetUserActive,
        ChangeUserRole,

        // Server responses
        Success,
        Error
    }

    // Basic request
    public class Packet
    {
        public PacketType Type { get; set; }
        public string Payload { get; set; } = "";  // JSON
        public string Token { get; set; } = "";     // session token after login
    }

    // Basic response
    public class Response
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Data { get; set; } = "";  // JSON
    }

    // Login
    public class LoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class LoginResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string Role { get; set; } = "";
        public string Token { get; set; } = "";
        public int Score { get; set; }
    }

    // Submit flag
    public class SubmitFlagRequest
    {
        public int ChallengeId { get; set; }
        public string Flag { get; set; } = "";
    }

    public class SubmitFlagResponse
    {
        public bool IsCorrect { get; set; }
        public int PointsEarned { get; set; }
        public int NewScore { get; set; }
        public string Message { get; set; } = "";
    }

    // Download file
    public class DownloadFileRequest
    {
        public int FileId { get; set; }
    }

    public class DownloadFileResponse
    {
        public string FileName { get; set; } = "";
        public string FileType { get; set; } = "";
        public byte[] FileData { get; set; } = Array.Empty<byte>();
    }
    // Upload file (Creator/Admin)
    public class UploadFileRequest
    {
        public int ChallengeId { get; set; }
        public string FileName { get; set; } = "";
        public string FileType { get; set; } = "";
        public byte[] FileData { get; set; } = Array.Empty<byte>();
    }
    public class DeleteFileRequest
    {
        public int FileId { get; set; }
    }   
// Create challenge (Creator/Admin)
    public class CreateChallengeRequest
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Flag { get; set; } = "";  // plain text — the server will encrypt
        public int Points { get; set; }
        public string Difficulty { get; set; } = "";
        public int CategoryId { get; set; }
    }

    // Update challenge (Creator/Admin)
    public class UpdateChallengeRequest
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Flag { get; set; } = "";  // empty = do not change the flag
        public int Points { get; set; }
        public string Difficulty { get; set; } = "";
        public int CategoryId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // Delete challenge (Admin only)
    public class DeleteChallengeRequest
    {
        public int Id { get; set; }
    }

    // Change user role (Admin only)
    public class ChangeUserRoleRequest
    {
        public int UserId { get; set; }
        public string NewRole { get; set; } = "";
    }

    // Unlock hint
    public class UnlockHintRequest
    {
        public int HintId { get; set; }
        public int ChallengeId { get; set; }
    }

    // Add hint (Creator/Admin)
    public class AddHintRequest
    {
        public int ChallengeId { get; set; }
        public string Content { get; set; } = "";
        public int PointCost { get; set; }
    }

    // Delete hint (Creator/Admin)
    public class DeleteHintRequest
    {
        public int HintId { get; set; }
    }

}