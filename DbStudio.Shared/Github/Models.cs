namespace DbStudio.Shared.Github.Models;

public record Release(string Url, int Id, string Name, string TagName) {
  public List<Asset> Assets { get; set; } = new();
}

public record Asset(string Name, string Browser_Download_Url, long Size);
