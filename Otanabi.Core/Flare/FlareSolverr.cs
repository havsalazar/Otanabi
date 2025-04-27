using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Otanabi.Core.Flare;

public class FlareSolverr
{
    internal string repo_name = "FlareSolverr";
    internal string repo_user = "FlareSolverr";
    internal string workingFolder = "flareSolverr";
    internal string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:126.0) Gecko/20100101 Firefox/126.0";
    internal bool enableInternalFlare = false;

    private static readonly HttpClient client = new();

    public async Task<bool> CheckFlareInstallation()
    {
        var flareInstalled = false;
        if (!CheckFlareExists())
        {
            var lastRelease = await GetLastRelease();
            if (!string.IsNullOrEmpty(lastRelease))
            {
                await DownloadLastRelease(lastRelease);
                flareInstalled = true;
            }
        }
        else
        {
            flareInstalled = true;
            await LaunchService();
        }

        return flareInstalled;
    }

    public bool CheckFlareExists()
    {
        var currDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        var flareFolder = Path.Combine(currDir, workingFolder);
        var flareFile = Path.Combine(flareFolder, "flaresolverr.exe");

        return File.Exists(flareFile);
    }

    internal async Task<string> GetLastRelease()
    {
        var url = $"https://api.github.com/repos/{repo_user}/{repo_name}/releases/latest";

        client.DefaultRequestHeaders.Add("User-Agent", UserAgent);

        try
        {
            var responseBody = await client.GetStringAsync(url);
            var release = JObject.Parse(responseBody);

            var tagName = release["tag_name"].ToString();
            var releaseName = release["name"].ToString();
            var releaseDate = release["published_at"].ToString();

            Debug.WriteLine($"Latest release: {releaseName} ({tagName})");
            Debug.WriteLine($"Published at: {releaseDate}");
            return tagName;
        }
        catch (HttpRequestException e)
        {
            Debug.WriteLine("\nException Caught!");
            Debug.WriteLine("Message :{0} ", e.Message);
        }
        return "";
    }

    internal async Task DownloadLastRelease(string release)
    {
        var currDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        var tempZipDir = Path.Combine(currDir, "tempzip");
        var zipLocation = Path.Combine(tempZipDir, "flaver-release.zip");
        var flareFolder = Path.Combine(currDir, workingFolder);
        var url =
            $"https://github.com/FlareSolverr/FlareSolverr/releases/download/{release}/flaresolverr_windows_x64.zip";

        Directory.CreateDirectory(tempZipDir);
        Directory.CreateDirectory(flareFolder);
        Debug.WriteLine("Downloading FlareSolverr ZIP");
        Debug.WriteLine($"Url: {url}");
        await DownloadFileAsync(url, zipLocation);
        Debug.WriteLine("Extracting FlareSolverr ZIP");
        await ExtractZipFile(zipLocation, currDir);
        Debug.WriteLine("moving extracted files to folder");
        //await MoveExtractedFiles(flareFolder);

        Directory.Delete(tempZipDir, true);
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

    internal async Task ExtractZipFile(string zipPath, string extractPath)
    {
        await Task.Run(() =>
        {
            ZipFile.ExtractToDirectory(zipPath, extractPath, true);
        });
    }

    internal async Task MoveExtractedFiles(string destinationPath)
    {
        var rootFolderPath = Path.Combine(destinationPath, "flaresolverr");

        var dirInfo = new DirectoryInfo(rootFolderPath);

        await Task.Run(() =>
        {
            var fileList = Directory.GetFiles(rootFolderPath, "*.*", SearchOption.AllDirectories);
            foreach (var file in fileList)
            {
                var mFile = new FileInfo(file);
                if (new FileInfo(dirInfo + "\\" + mFile.Name).Exists == false)
                {
                    mFile.MoveTo(dirInfo + "\\" + mFile.Name);
                }
            }
        });
    }

    private void KillChromiumOrphans()
    {
        var chromiums = Process.GetProcessesByName("chrome");
        foreach (var proc in chromiums)
        {
            if (proc == null)
                continue;
            try
            {
                if (proc.MainModule.FileName.Contains("flare"))
                    proc.Kill();
            }
            catch (Exception) { }
        }
        var cDriver = Process.GetProcessesByName("chromedriver");
        foreach (var proc in cDriver)
        {
            try
            {
                proc.Kill();
            }
            catch (Exception) { }
        }
    }

    private async Task LaunchService()
    {
        if (!enableInternalFlare)
        {
            return;
        }

        var currDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        var flareFolder = Path.Combine(currDir, workingFolder);
        var flareFile = Path.Combine(flareFolder, "flaresolverr.exe");

        var pname = Process.GetProcessesByName("flaresolverr");
        await Task.Run(() => KillChromiumOrphans());
        if (pname.Length > 0)
        {
            foreach (var proc in pname)
            {
                proc.Kill();
            }
            await Task.Run(() => KillChromiumOrphans());
        }

        var bw = new BackgroundWorker();
        bw.DoWork += (sender, args) =>
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = flareFile;

            startInfo.UseShellExecute = false;
            startInfo.Arguments = "/s /v /qn /min";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.CreateNoWindow = true;
            try
            {
                var process = Process.Start(startInfo);
                AppDomain.CurrentDomain.DomainUnload += (s, e) =>
                {
                    process.Kill();
                    process.WaitForExit();
                    KillChromiumOrphans();
                };
                AppDomain.CurrentDomain.ProcessExit += (s, e) =>
                {
                    process.Kill();
                    process.WaitForExit();
                    KillChromiumOrphans();
                };
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    process.Kill();
                    process.WaitForExit();
                    KillChromiumOrphans();
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        };
        bw.RunWorkerAsync();
        await Task.CompletedTask;
    }
}
