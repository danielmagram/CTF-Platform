using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CTF.Common.Services
{
    public class ValidationService
    {
        // Flag format: CTF{...}
        private static readonly Regex FlagRegex = new(@"^CTF\{[a-zA-Z0-9_\-!@#$}{%+=^&*]+\}$");

        // Username: 3-20 characters, letters/numbers/underscore
        private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_]{3,20}$");

        // Password: at least 6 characters
        private static readonly Regex PasswordRegex = new(@"^.{6,64}$");

        // Allowed file types for challenges
        private static readonly HashSet<string> AllowedFileTypes = new()
        {
            ".zip", ".tar", ".gz", ".pcap", ".pcapng",
            ".exe", ".elf", ".bin", ".py", ".txt",
            ".png", ".jpg", ".pdf"
        };

        public static ValidationResult ValidateFlag(string flag)
        {
            if (string.IsNullOrWhiteSpace(flag))
                return new ValidationResult(false, "Flag cannot be empty");

            if (!FlagRegex.IsMatch(flag))
                return new ValidationResult(false, "Flag format must be CTF{...}");

            return new ValidationResult(true, "Valid");
        }

        public static ValidationResult ValidateUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return new ValidationResult(false, "Username cannot be empty");

            if (!UsernameRegex.IsMatch(username))
                return new ValidationResult(false, "Username: 3-20 chars, letters/numbers/underscore only");

            return new ValidationResult(true, "Valid");
        }

        public static ValidationResult ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return new ValidationResult(false, "Password cannot be empty");

            if (!PasswordRegex.IsMatch(password))
                return new ValidationResult(false, "Password must be 6-64 characters");

            return new ValidationResult(true, "Valid");
        }

        public static ValidationResult ValidateFileType(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLower();

            if (!AllowedFileTypes.Contains(ext))
                return new ValidationResult(false, $"File type '{ext}' is not allowed");

            return new ValidationResult(true, "Valid");
        }

        public static ValidationResult ValidateChallengeTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return new ValidationResult(false, "Title cannot be empty");

            if (title.Length < 3 || title.Length > 100)
                return new ValidationResult(false, "Title must be 3-100 characters");

            return new ValidationResult(true, "Valid");
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; }
        public string Message { get; }

        public ValidationResult(bool isValid, string message)
        {
            IsValid = isValid;
            Message = message;
        }
    }
}
