using System.Diagnostics;
namespace Otanabi.Core.Services;
public class ServerService {
    private readonly string  currDir = Path.GetDirectoryName(
        System.Reflection.Assembly.GetExecutingAssembly().Location
    );
    private readonly string serverPath =  "Otanabi.Server.exe";

    public async Task GetServerInfo() {
    }
    public async Task InitServer()
    {
        if (IsServerRunning())
        {
            Console.WriteLine("Server is already running");
            return;
        }

        var serverProccess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(currDir, serverPath),
                UseShellExecute = false,
                //RedirectStandardOutput = true,
                //CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };
        serverProccess.Exited += (sender, e) =>
        {
            Console.WriteLine("Server exited");
        };
        serverProccess.Start();
        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            if (!serverProccess.HasExited)
            {
                serverProccess.Kill();
            }
        };
    }
    private bool IsServerRunning()
    {
        return Process.GetProcessesByName(serverPath).Length > 0;
    }
}
