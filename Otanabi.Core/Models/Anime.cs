using System;
using System.Diagnostics;
using SQLite;

namespace Otanabi.Core.Models;

[DebuggerDisplay("Title : {Title} , Prov: {Provider.Name}")]
public enum AnimeType
{
    OVA,
    TV,
    MOVIE,
    SPECIAL,
    OTHER,
}

public class Anime : IAnime
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int IdAnilist { get; set; }
    public string RemoteID { get; set; }
    public string Cover { get; set; }
    public string Title { get; set; }
    public string Url { get; set; }
    public AnimeType Type { get; set; }
    public string Description { get; set; }
    public int ProviderId { get; set; }
    public string Status { get; set; }
    public DateTime LastUpdate { get; set; }
    public string TypeStr => Type.ToString();
    public string GenreStr { get; set; }

    public string AlternativeTitlesStr { get; set; } = string.Empty;

    [Ignore]
    public List<string> AlternativeTitles =>
        (!string.IsNullOrEmpty(AlternativeTitlesStr))
            ? AlternativeTitlesStr.Split(new string[] { "!-!" }, StringSplitOptions.None).ToList()
            : new();

    [Ignore]
    public ICollection<Chapter> Chapters { get; set; }

    [Ignore]
    public Provider Provider { get; set; }

    [Ignore]
    public List<string> Genre => (GenreStr != null) ? GenreStr.Split(',').ToList() : new();
}
