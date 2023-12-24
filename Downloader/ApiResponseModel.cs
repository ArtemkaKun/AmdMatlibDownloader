using System.Text.Json.Serialization;

namespace Downloader;

public class ApiResponseModel
{
    public int Count { get; init; }
    public string Next { get; init; }
    public List<MaterialData> Results { get; init; }
}

public class MaterialData
{
    public List<string> Packages { get; init; }
    public string Title { get; init; }
}

public class PackageData
{
    [JsonPropertyName("file_url")]
    public string FileUrl { get; init; }
    
    public string File { get; init; }
}