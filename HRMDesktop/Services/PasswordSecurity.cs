using System;
using System.Security.Cryptography;
using HRMDesktop.Models;

namespace HRMDesktop.Services
{
    public static class PasswordSecurity
    {
        public static UserCredential CreateCredential(int employeeId, string username, string password, string role)
        {
            byte[] saltBytes = new byte[16];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(saltBytes);
            string salt = Convert.ToBase64String(saltBytes);
            return new UserCredential
            {
                EmployeeId = employeeId,
                Username = username.Trim().ToLowerInvariant(),
                PasswordSalt = salt,
                PasswordHash = Hash(password, salt),
                Role = role,
                AttendanceNotificationEnabled = true,
                LeaveNotificationEnabled = true,
                SalaryNotificationEnabled = true
            };
        }

        public static bool Verify(string password, UserCredential credential)
        {
            if (credential == null || credential.IsLocked) return false;
            return SlowEquals(credential.PasswordHash, Hash(password ?? string.Empty, credential.PasswordSalt));
        }

        private static string Hash(string password, string salt)
        {
            byte[] saltBytes = Convert.FromBase64String(salt);
            using (var deriveBytes = new Rfc2898DeriveBytes(password, saltBytes, 100000))
            {
                return Convert.ToBase64String(deriveBytes.GetBytes(32));
            }
        }

        private static bool SlowEquals(string left, string right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            int difference = 0;
            for (int i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }
    }
}
