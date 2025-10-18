using System.IO.Compression;
using System.Runtime.CompilerServices;
using LibGit2Sharp;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

static string GetFilePath([CallerFilePath] string path = "") => path;

string filePath = GetFilePath();
string basePath = new FileInfo(filePath).Directory?.Parent?.FullName!;

const string url = "https://reqrypt.org/download/WinDivert-2.2.2-A.zip";
const string packageName = "Native.WinDivert";
string projectPath = Path.Combine(basePath, ".local", packageName);
string publishPath = Path.Combine(basePath, ".local", "Publish");

// Get metadata from Git using LibGit2Sharp.
string userName;
string? repositoryUrl;
using (var repo = new Repository(basePath))
{
    userName = repo.Config.Get<string>("user.name").Value;
    repositoryUrl = repo.Network.Remotes.FirstOrDefault()?.Url;
}

if (Directory.Exists(projectPath))
{
    Directory.Delete(projectPath, recursive: true);
}
if (Directory.Exists(publishPath))
{
    Directory.Delete(publishPath, recursive: true);
}
Directory.CreateDirectory(projectPath);
Directory.CreateDirectory(publishPath);

// Download and extract binary files.
Console.WriteLine("Downloading WinDivert...");
using var httpClient = new HttpClient();
using var stream = await httpClient.GetStreamAsync(url);
using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
archive.ExtractToDirectory(projectPath);
string binaryPath = new DirectoryInfo(projectPath).EnumerateDirectories().Single().FullName;
string version = File.ReadAllText(Path.Combine(binaryPath, "VERSION")).Trim();

Console.WriteLine($"Creating NuGet package...");
var packageBuilder = new PackageBuilder
{
    Authors = { userName },
    Version = new NuGetVersion(version),
    Id = packageName,
    Description = "WinDivert binary files.",
    Readme = "ReadMe.md",
    LicenseMetadata = new LicenseMetadata(LicenseType.File, "LICENSE", null, null, LicenseMetadata.EmptyVersion),
    RequireLicenseAcceptance = true,
    Repository = repositoryUrl is null ? null : new RepositoryMetadata { Url = repositoryUrl },
    Tags = { "WinDivert" },
};
packageBuilder.PopulateFiles(
    "/",
    [
        new ManifestFile { Source = Path.Combine(basePath, "ReadMe.md"), Target = "ReadMe.md" },
        new ManifestFile { Source = Path.Combine(binaryPath, "LICENSE"), Target = "LICENSE" },
        new ManifestFile
        {
            Source = Path.Combine(binaryPath, "x64", "WinDivert.dll"),
            Target = "runtimes/win-x64/native",
        },
        new ManifestFile
        {
            Source = Path.Combine(binaryPath, "x64", "WinDivert64.sys"),
            Target = "runtimes/win-x64/native",
        },
    ]
);
using var package = File.OpenWrite(Path.Combine(publishPath, $"{packageName}.{version}.nupkg"));
packageBuilder.Save(package);

Console.WriteLine("Done.");
