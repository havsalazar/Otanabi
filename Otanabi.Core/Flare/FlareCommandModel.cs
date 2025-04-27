namespace Otanabi.Core.Flare;

public class FlareCommandModel
{
    public string cmd { get; set; }
    public string url { get; set; }
    public long maxTimeout { get; set; }
}

public class Session
{
    public string session { get; set; }
}

public class SessionListResp : StatusInfo
{
    public List<string> sessions { get; set; }
}

public class SesionCreated : StatusInfo
{
    public string session { get; set; }
}

public class StatusInfo
{
    public string status { get; set; }
    public string message { get; set; }
    public long startTimestamp { get; set; }
    public long endTimestamp { get; set; }
    public string version { get; set; }
}

public class GetResponse : StatusInfo
{
    public Solution solution { get; set; }
}

public class Solution
{
    public string response { get; set; }
    public string url { get; set; }
    public long status { get; set; }
}
