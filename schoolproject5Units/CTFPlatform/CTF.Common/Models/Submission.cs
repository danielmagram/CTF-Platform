using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTF.Common.Models
{
    public class Submission
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public int ChallengeId { get; set; }
        public string ChallengeTitle { get; set; } = "";
        public string SubmittedFlag { get; set; } = "";
        public bool IsCorrect { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class HintUnlock
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int HintId { get; set; }
        public DateTime UnlockedAt { get; set; }
    }

    public class ScoreboardEntry
    {
        public int Rank { get; set; }
        public string Username { get; set; } = "";
        public int Score { get; set; }
        public int SolvedCount { get; set; }
        public DateTime LastSolve { get; set; }
    }
}