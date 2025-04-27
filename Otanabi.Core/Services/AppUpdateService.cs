using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Otanabi.Core.Helpers;

namespace Otanabi.Core.Services;

public sealed class AppUpdateService
{
    private readonly HttpService _http = new();
    private readonly ClassReflectionHelper reflectionHelper = new();
    private readonly string gitUrl = "https://raw.githubusercontent.com/havsalazar/Otanabi/master/Otanabi/version.v";
    private readonly string gitRelease = "https://api.github.com/repos/havsalazar/Otanabi/releases";
    internal string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:126.0) Gecko/20100101 Firefox/126.0";

    public async Task<string> CheckGitHubVersion()
    {
        var response = await _http.GetAsync(gitUrl);
        return response;
    }

    public async Task<string> GetReleaseNotes()
    {
        var response = await _http.GetAsync($"{gitRelease}/latest");
        return response;
    }

    public async Task<string> GetReleaseNotes(string version)
    {
        var response = await _http.GetAsync($"{gitRelease}/tags/v{version}");
        return response;
    }

    public string GetCurrVersion()
    {
        var currDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        var versionPath = Path.Combine(currDir, "version.v");
        var line1 = File.ReadLines(versionPath).First();
        return line1;
    }

    public async Task<(int, Version)> CheckMainUpdates()
    {
        var gitResponse = await CheckGitHubVersion();
        var gitVersion = new Version(gitResponse);
        var currDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        var versionPath = Path.Combine(currDir, "version.v");
        var line1 = File.ReadLines(versionPath).First();
        var currVersion = new Version(line1);
        var result = currVersion.CompareTo(gitVersion);
        return (result, gitVersion);
    }

    public async Task UpdateApp()
    {
        var tag = await GetLastReleaseTag();
        var updateUrl = $"https://github.com/havsalazar/Otanabi/releases/download/{tag}/Otanabi-{tag}-x64.zip";
        var currDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

        await DownloadAndInstallUpdate(updateUrl, currDir);
    }

    public Version GetExtensionVer()
    {
        var assemblyPath = reflectionHelper.GetAssemblyPath();
        var fvi = FileVersionInfo.GetVersionInfo(assemblyPath);
        return new Version(fvi.FileVersion);
    }

    public static async Task DownloadAndInstallUpdate(string url, string destinationFolder)
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "animeupdate.zip");

        await DownloadFileAsync(url, tempFile);
        var ps1File = Path.Combine(destinationFolder, "update.ps1");
        var startInfo = new ProcessStartInfo()
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy ByPass -File \"{ps1File}\"",
            UseShellExecute = false,
        };
        Process.Start(startInfo);
    }

    internal static async Task DownloadFileAsync(string url, string outputPath)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(3);
        using var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        using Stream contentStream = await response.Content.ReadAsStreamAsync(),
            fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
        await contentStream.CopyToAsync(fileStream);
    }

    internal async Task<string> GetLastReleaseTag()
    {
        var url = $"{gitRelease}/latest";
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", UserAgent);

        try
        {
            var responseBody = await client.GetStringAsync(url);
            var release = JObject.Parse(responseBody);

            var tagName = release["tag_name"].ToString();
            var releaseName = release["name"].ToString();
            var releaseDate = release["published_at"].ToString();
            return tagName;
        }
        catch (HttpRequestException e)
        {
            Debug.WriteLine("\nException Caught!");
            Debug.WriteLine("Message :{0} ", e.Message);
        }
        return "";
    }

    public async Task<bool> IsNeedUpdate()
    {
        var result = await CheckMainUpdates();
        return result.Item1 < 0;
    }
}
