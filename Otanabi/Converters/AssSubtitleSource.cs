using System.Formats.Tar;
using System.Security.Cryptography;
using System.Text;

namespace Otanabi.Converters;

public class AssSubtitleSource
{
    public static async Task<string> SaveSrtToTempFolderAsync(string url)
    {
        var guid = sha256_hash(url);
        var tempFolder = Path.GetTempPath();
        var assFile = Path.Combine(tempFolder, $"{guid}.ass");
        if (!File.Exists(assFile))
        {
            var assContent = await DownloadAssFileAsync(url);
            //using (File.WriteAllText(Path.Combine(tempFolder, "subtitles.ass"), assContent))
            using (StreamWriter writer = new StreamWriter(Path.Combine(tempFolder, $"{guid}.ass"), false))
            {
                writer.Write(assContent);
            }
            await Task.CompletedTask;
        }
        return assFile;
    }

    private static async Task<string> DownloadAssFileAsync(string url)
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public static string sha256_hash(string value)
    {
        var Sb = new StringBuilder();

        using (SHA256 hash = SHA256.Create())
        {
            var enc = Encoding.UTF8;
            var result = hash.ComputeHash(enc.GetBytes(value));

            foreach (var b in result)
                Sb.Append(b.ToString("x2"));
        }

        return Sb.ToString();
    }
}
