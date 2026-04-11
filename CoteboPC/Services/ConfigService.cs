using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoteboPC;

public static class ConfigService
{
    private static string ConfigPath => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CoteboPC", "config.json")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "CoteboPC", "config.json");

    public static bool ConfigExists() => File.Exists(ConfigPath);

    public static async Task SaveConfig(string token, long chatId)
    {
        var dir = Path.GetDirectoryName(ConfigPath)!;
        Directory.CreateDirectory(dir);

        var config = new AppConfig
        {
            Token = Encrypt(token),
            ChatId = chatId
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(ConfigPath, json);
    }

    public static async Task<AppConfig?> LoadConfig()
    {
        if (!File.Exists(ConfigPath)) return null;

        var json = await File.ReadAllTextAsync(ConfigPath);
        var config = JsonSerializer.Deserialize<AppConfig>(json);

        if (config?.Token != null)
            config.Token = Decrypt(config.Token);

        return config;
    }

    private static string Encrypt(string plainText)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            byte[] data = Encoding.UTF8.GetBytes(plainText);
            byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }
        else
        {
            // Простое шифрование для Linux
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes("CoteboPC2026SecretKey12345678"); // 32 байта
            aes.IV = new byte[16];
            using var encryptor = aes.CreateEncryptor();
            byte[] data = Encoding.UTF8.GetBytes(plainText);
            byte[] encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
            return Convert.ToBase64String(encrypted);
        }
    }

    private static string Decrypt(string encryptedText)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            byte[] encrypted = Convert.FromBase64String(encryptedText);
            byte[] data = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }
        else
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes("CoteboPC2026SecretKey12345678");
            aes.IV = new byte[16];
            using var decryptor = aes.CreateDecryptor();
            byte[] encrypted = Convert.FromBase64String(encryptedText);
            byte[] data = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
            return Encoding.UTF8.GetString(data);
        }
    }
}
