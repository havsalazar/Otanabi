using System.Diagnostics;
using Otanabi.Server.Routes;
namespace Otanabi.Server;

public class Program
{
    private static readonly TorrentController torrentController = new();
    public static void Main(string[] args)
    {
        var startTimeSpan = TimeSpan.Zero;
        var periodTimeSpan = TimeSpan.FromMinutes(1);

        var timer = new Timer((e) =>
        {
            CloseServer();   
        }, null, startTimeSpan, periodTimeSpan);

        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls("http://localhost:8890");
        var app = builder.Build();

        app.MapGet("/", () => "Hello World!");
        app.MapGet("/torrents", () => torrentController.GetTorrents());

        app.Run();
    }


    // Closes the server when Otanabi is closed
    private static void CloseServer()
    {
        var serverProcess = Process.GetProcessesByName("Otanabi");
        if (serverProcess.Length == 0)
        {
            Environment.Exit(0);
        }
    }
}
