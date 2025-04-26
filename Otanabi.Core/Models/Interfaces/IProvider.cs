namespace Otanabi.Core.Models;

public interface IProvider
{
    Anime[] Animes { get; set; }
    int Id { get; set; }
    string Name { get; set; }
    bool Persistent { get; set; }
    string Type { get; set; }
    string Url { get; set; }
}
