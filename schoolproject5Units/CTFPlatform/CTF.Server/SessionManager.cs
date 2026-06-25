using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CTF.Common.Models;

namespace CTF.Server
{
    public class SessionManager
    {
        // token -> User
        private readonly Dictionary<string, User> _sessions = new();
        private readonly object _lock = new();

        public string CreateSession(User user)
        {
            string token = Common.Services.CryptoService.GenerateToken();
            lock (_lock)
                _sessions[token] = user;
            return token;
        }

        public User? GetUser(string token)
        {
            lock (_lock)
                return _sessions.TryGetValue(token, out User? user) ? user : null;
        }

        public void RemoveSession(string token)
        {
            lock (_lock)
                _sessions.Remove(token);
        }

        public bool IsLoggedIn(string token) => GetUser(token) != null;
    }
}
