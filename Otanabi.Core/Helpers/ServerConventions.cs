using Otanabi.Core.Models;

namespace Otanabi.Core.Helpers;

public class ServerConventions
{
    internal List<Convention> Conventions =
        [
            new Convention
            {
                Name = "Okru",
                PossibleNames = ["ok-ru", "okru", "OKRU", "ok"]
            },
            new Convention
            {
                Name = "Streamwish",
                PossibleNames = ["sw", "SW", "Streamwish", "streamwish", "wish"]
            },
            new Convention
            {
                Name = "Streamtape",
                PossibleNames = ["stape", "Stape","Streamtape","streamtape"]
            },
            new Convention { 
                Name = "Juro", 
                PossibleNames = ["juro"] 
            },
            new Convention {
                Name = "StreamHideVid",
                PossibleNames = ["vidhide", "VidHidePro", "luluvdo", "vidhideplus", "Earnvids", "streamvid", "guccihide", "streamhide"]
            },
            new Convention {
                Name = "Filemoon",
                PossibleNames = ["filemoon", "fmoon", "moon", "moonplayer"]
            },
            new Convention {
                Name = "Fastream",
                PossibleNames = ["fastream"]
            },
            new Convention {
                Name = "SendVid",
                PossibleNames = ["SendVid", "Send", "sendvid"]
            },
            new Convention {
                Name = "VidHide",
                PossibleNames = ["vidhide", "filelions.top", "vid.", "nika", "niikaplayerr"]
            },
            new Convention {
                Name = "VidGuard",
                PossibleNames = ["listeamed", "VidGuard", "vidg", "vembed", "guard", "bembed", "vgfplay"]
            },
            //new Convention
            //{
            //    Name = "Yourupload",
            //    PossibleNames = new string[] { "yourupload", "Yourupload", "yu", "YU" }
            //}
        ];

    public string GetServerName(string serverName)
    {
        var convention = "";
        try
        {
            var lowerServerName = serverName.ToLowerInvariant();
            convention = Conventions.First(e => e.PossibleNames.Any(name => name.Equals(lowerServerName, StringComparison.OrdinalIgnoreCase))).Name;
        }
        catch (Exception) { }
        return convention;
    }
}
