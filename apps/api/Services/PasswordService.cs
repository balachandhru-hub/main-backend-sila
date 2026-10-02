using Microsoft.AspNetCore.Identity;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public interface IPasswordService
{
    string HashPassword(User user, string password);
    bool VerifyPassword(User user, string password);
}

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<User> hasher = new();

    public string HashPassword(User user, string password) => hasher.HashPassword(user, password);

    public bool VerifyPassword(User user, string password)
    {
        if (string.IsNullOrWhiteSpace(user.PasswordHash) || password is null)
        {
            return false;
        }

        try
        {
            return hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}