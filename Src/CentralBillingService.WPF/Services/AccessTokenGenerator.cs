using System.Security.Cryptography;

namespace CentralBillingService.WPF.Services;

public static class AccessTokenGenerator
{
    private const int TokenByteLength = 32;

    public static string GenerateAccessToken()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        string token = Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return token;
    }
}
