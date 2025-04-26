namespace Otanabi.Core.Models;

public interface IAnime
{
    ICollection<Chapter> Chapters { get; set; }
    string Cover { get; set; }
    string Description { get; set; }
    int Id { get; set; }
    DateTime LastUpdate { get; set; }
    Provider Provider { get; set; }
    int ProviderId { get; set; }
    string RemoteID { get; set; }
    string Status { get; set; }
    string Title { get; set; }
    AnimeType Type { get; set; }
    string TypeStr { get; }
    string Url { get; set; }
    string GenreStr { get; set; }
    List<string> Genre { get; }
}
