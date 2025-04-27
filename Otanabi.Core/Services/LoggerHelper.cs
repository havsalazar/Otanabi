using NLog;

namespace Otanabi.Core.Services;

public sealed class LoggerService
{
    private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

    public void LogDebug(string message, params object[] args) => logger.Debug(message, args);

    public void LogInfo(string message, params object[] args) => logger.Info(message, args);

    public void LogWarn(string message, params object[] args) => logger.Warn(message, args);

    public void LogError(string message, params object[] args) => logger.Error(message, args);

    public void LogFatal(string message, params object[] args) => logger.Fatal(message, args);
}
