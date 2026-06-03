using System.Security.Cryptography;

namespace OllamaSharp.RAG.SQLite.Helpers;

public static class HashHelper
{
    public static string GetFileHash(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found.", filePath);
        }

        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        byte[] hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }
}