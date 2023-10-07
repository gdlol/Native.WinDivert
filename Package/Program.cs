using System.ComponentModel;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.Cli.Utils;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

static string GetFilePath([CallerFilePath] string? path = null)
{
    if (path is null)
    {
        throw new InvalidOperationException(nameof(path));
    }
    return path;
}

static string GetOutput(string commandName, params string[] args)
{
    var command = Command.Create(commandName, args).CaptureStdOut();
    var result = command.Execute();
    if (result.ExitCode != 0)
    {
        throw new Win32Exception(result.ExitCode);
    }
    return result.StdOut;
}

const string url = "https://reqrypt.org/download/WinDivert-2.2.2-A.zip";
const string packageName = "Native.WinDivert";

// Get metadata from Git.
string userName = GetOutput("git", "config", "user.name").Trim();
var remotes = GetOutput("git", "remote").Trim();
string? repositoryUrl = remotes.Split('\n').FirstOrDefault() switch
{
    null or "" => null,
    string remote => GetOutput("git", "remote", "get-url", remote.Trim()).Trim()
};

string filePath = GetFilePath();
string basePath = new FileInfo(filePath).Directory?.Parent?.FullName!;
string projectPath = Path.Combine(basePath, packageName);
string publishPath = Path.Combine(basePath, "Publish");
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
using var httpClient = new HttpClient();
using var stream = await httpClient.GetStreamAsync(url);
using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
archive.ExtractToDirectory(projectPath);
string binaryPath = new DirectoryInfo(projectPath).EnumerateDirectories().Single().FullName;
string version = File.ReadAllText(Path.Combine(binaryPath, "VERSION")).Trim();

var packageBuilder = new PackageBuilder
{
    Authors = { userName },
    Version = new NuGetVersion(version),
    Id = packageName,
    Description = "WinDivert binary files.",
    Readme = "ReadMe.md",
    LicenseMetadata = new LicenseMetadata(LicenseType.File, "LICENSE", null, null, LicenseMetadata.EmptyVersion),
    RequireLicenseAcceptance = true,
    Repository = repositoryUrl is null ? null : new RepositoryMetadata
    {
        Url = repositoryUrl
    },
    Tags = { "WinDivert" }
};
packageBuilder.PopulateFiles(
    new FileInfo(filePath).Directory!.FullName,
    new[]
    {
        new ManifestFile
        {
            Source = Path.Combine(basePath, "ReadMe.md"),
            Target = "ReadMe.md"
        },
        new ManifestFile
        {
            Source = Path.Combine(binaryPath, "LICENSE"),
            Target = "LICENSE"
        },
        new ManifestFile
        {
            Source = Path.Combine(binaryPath, "x64", "WinDivert.dll"),
            Target = "runtimes/win-x64/native"
        },
        new ManifestFile
        {
            Source = Path.Combine(binaryPath, "x64", "WinDivert64.sys"),
            Target = "runtimes/win-x64/native"
        }
    });
using var package = File.OpenWrite(Path.Combine(publishPath, $"{packageName}.{version}.nupkg"));
packageBuilder.Save(package);

Console.WriteLine("Done.");
