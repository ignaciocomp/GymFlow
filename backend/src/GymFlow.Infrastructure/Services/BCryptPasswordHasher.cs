using GymFlow.Application.Interfaces;

namespace GymFlow.Infrastructure.Services;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            throw new ArgumentException("Password is required.", nameof(plainPassword));
        return BCrypt.Net.BCrypt.HashPassword(plainPassword);
    }

    public bool Verify(string plainPassword, string hash)
    {
        if (string.IsNullOrWhiteSpace(plainPassword) || string.IsNullOrWhiteSpace(hash))
            return false;
        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, hash);
        }
        catch
        {
            return false; // hash inválido
        }
    }
}
