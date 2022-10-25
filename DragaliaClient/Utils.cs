using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Utilities.Net;
using IPAddress = System.Net.IPAddress;

namespace DragaliaClient;

public static class Utils
{
    private static readonly char[] AllowedChars =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public static string GetRandomString(int length = 50)
    {
        var sb = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            sb.Append(AllowedChars[RandomNumberGenerator.GetInt32(AllowedChars.Length)]);
        }

        return sb.ToString();
    }

    public static string HashString(string data)
    {
        var hashed = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return UrlSafeB64(hashed);
    }

    public static string UrlSafeB64(byte[] input)
        => Convert.ToBase64String(input).Replace("+", "-").Replace("/", "_").Replace("=", "");

    public static string UrlSafeB64(string input)
        => UrlSafeB64(Encoding.UTF8.GetBytes(input));

    public static string UrlEncode(Dictionary<string, string> dict)
    {
        if (dict.Count == 0)
            return "";

        var pairs = dict.ToList();
        var sb = new StringBuilder();

        sb.Append($"?{Uri.EscapeDataString(pairs.First().Key)}={Uri.EscapeDataString(pairs.First().Value)}");
        pairs.RemoveAt(0);

        if (pairs.Count < 1) 
            return sb.ToString();

        while (pairs.Count != 0)
        {
            var pair = pairs.First();
            sb.Append($"&{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}");
            pairs.RemoveAt(0);
        }

        return sb.ToString();
    }

    public static string GenerateAssertion(string packageName = "com.nintendo.zaga",
        string packageSignature = "0c3d789f5ed23f2b34c79660a37190d1c873a3f2", string audience = Constants.BaaSHost)
    {
        const int lifespanSeconds = 600;

        var issuer = $"{packageName}:{packageSignature}";

        var time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000 / lifespanSeconds;
        var timeStr = Convert.ToString(time, 16).ToUpper();
        while (timeStr.Length < 16)
            timeStr = "0" + timeStr;

        var hashData = Convert.FromHexString(timeStr);
        var hash = HMACSHA1.HashData(Encoding.UTF8.GetBytes(issuer), hashData);

        var offset = hash[^1] & 0xf;
        var key = (
            (hash[offset + 3]
             | hash[offset + 2] << 8
             | hash[offset + 1] << 16
             | ((hash[offset] & 0x7f) << 24)
            ) % 100000000
            ).ToString();

        while (key.Length < 8)
            key = "0" + key;

        var jwtHeader = new Dictionary<string, string> {{"alg", "HS256"}};
        var jwtBody = new Dictionary<string, string>
        {
            {"iss", issuer}, 
            {"iat", (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000).ToString()},
            {"aud", audience}
        };

        var jwtString =
            $"{UrlSafeB64(JsonSerializer.Serialize(jwtHeader))}.{UrlSafeB64(JsonSerializer.Serialize(jwtBody))}";

        var signature = HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(jwtString));

        return $"{jwtString}.{UrlSafeB64(signature)}";
    }
}