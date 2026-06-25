using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTF.Common.Models
{
    public class Challenge
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string EncryptedFlag { get; set; } = "";
        public int Points { get; set; }
        public DifficultyLevel Difficulty { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public int CreatorId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public List<Hint> Hints { get; set; } = new();
        public List<ChallengeFile> Files { get; set; } = new();
    }

    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard,
        Insane
    }

    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class Hint
    {
        public int Id { get; set; }
        public int ChallengeId { get; set; }
        public string Content { get; set; } = "";
        public int PointCost { get; set; }
        public bool IsUnlocked { get; set; }
    }

    public class ChallengeFile
    {
        public int Id { get; set; }
        public int ChallengeId { get; set; }
        public string FileName { get; set; } = "";
        public string FileType { get; set; } = "";
        public byte[] FileData { get; set; } = Array.Empty<byte>();
    }
}