using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTF.Common.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public UserRole Role { get; set; }
        public int Score { get; set; }
        public DateTime RegisteredAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public enum UserRole
    {
        Player,
        Creator,
        Admin
    }
}
