using Microsoft.AspNetCore.Identity;

namespace StudentRegistrationPortal.Helpers
{
    public static class PasswordHelper
    {
        private static PasswordHasher<string> hasher = new();

        public static string HashPassword(string password)
        {
            return hasher.HashPassword(null, password);
        }

        public static bool VerifyPassword(string hash, string password)
        {
            return hasher.VerifyHashedPassword(null, hash, password)
                   == PasswordVerificationResult.Success;
        }
    }
}
