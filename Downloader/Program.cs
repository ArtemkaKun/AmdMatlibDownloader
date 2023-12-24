using System.Net;
using System.Text.Json;
using Downloader;

var handler = new HttpClientHandler
{
    AllowAutoRedirect = true,
    UseCookies = true,
    CookieContainer = new CookieContainer()
};

using var httpClient = new HttpClient(handler);
httpClient.BaseAddress = new Uri("https://api.matlib.gpuopen.com/api/");
httpClient.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:121.0) Gecko/20100101 Firefox/121.0");
httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");

var firstResponse = await httpClient.GetAsync("materials/?status=Published&updateKey=1&ordering=+title&limit=1");
firstResponse.EnsureSuccessStatusCode();

var firstResponseContent = await firstResponse.Content.ReadAsStringAsync();

var firstApiResponse = JsonSerializer.Deserialize<ApiResponseModel>(firstResponseContent, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
});

if (firstApiResponse == null)
{
    Console.WriteLine("Failed to deserialize first response");
    return;
}

var allMaterialsResponse = await httpClient.GetAsync($"materials/?status=Published&updateKey=1&ordering=+title&limit={firstApiResponse.Count}");
allMaterialsResponse.EnsureSuccessStatusCode();

var allMaterialsResponseContent = await allMaterialsResponse.Content.ReadAsStringAsync();

var allApiResponse = JsonSerializer.Deserialize<ApiResponseModel>(allMaterialsResponseContent, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
});

if (allApiResponse == null)
{
    Console.WriteLine("Failed to deserialize all response");
    return;
}

var currentDirectory = Directory.GetCurrentDirectory();
var materialsDirectory = Path.Combine(currentDirectory, "Materials");

if (Directory.Exists(materialsDirectory) == false)
{
    Directory.CreateDirectory(materialsDirectory);
}

var allMaterials = allApiResponse.Results;

foreach (var material in allMaterials)
{
    var materialDirectory = Path.Combine(materialsDirectory, material.Title);

    if (Directory.Exists(materialDirectory) == false)
    {
        Directory.CreateDirectory(materialDirectory);
    }

    foreach (var package in material.Packages)
    {
        var packageDataResponse = await httpClient.GetAsync($"packages/{package}/");
        packageDataResponse.EnsureSuccessStatusCode();

        var packageDataResponseContent = await packageDataResponse.Content.ReadAsStringAsync();

        var packageData = JsonSerializer.Deserialize<PackageData>(packageDataResponseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (packageData == null)
        {
            Console.WriteLine($"Failed to deserialize package data for {material.Title} - {package}");
            continue;
        }

        if (string.IsNullOrWhiteSpace(packageData.FileUrl))
        {
            Console.WriteLine($"No file url for {material.Title} - {package}");
            continue;
        }

        var packagePath = Path.Combine(materialDirectory, packageData.File);

        if (File.Exists(packagePath))
        {
            Console.WriteLine($"Package already exists for {material.Title} - {package}");
            continue;
        }

        var packageFileResponse = await httpClient.GetAsync(packageData.FileUrl);

        if (packageFileResponse.IsSuccessStatusCode == false)
        {
            Console.WriteLine($"Failed to download package for {material.Title} - {package}");
            Console.WriteLine(packageFileResponse.StatusCode);
            Console.WriteLine(packageFileResponse.ReasonPhrase);
            continue;
        }

        await using var packageFileStream = File.Create(packagePath);

        await packageFileResponse.Content.CopyToAsync(packageFileStream);

        Console.WriteLine($"Downloaded package for {material.Title} - {package}");
    }
}