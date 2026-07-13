using System.Security.Cryptography;
using System.Text;

namespace TreeDrive.Infrastructure.Helpers;

public static class PasswordHelper
{
    // Hash a password using BCrypt (industry standard)
    public static string HashPassword(string password)
    {
        // BCrypt automatically handles salting
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    // Verify a password against a hash
    public static bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}