using System.Xml.Linq;

namespace SubTrack.ArchitectureTests;

/// <summary>
/// Reads the solution layout straight from disk (<c>.csproj</c> and <c>.slnx</c> files), so the checks also cover
/// references that the compiler would drop from the built assemblies because nothing uses them yet.
/// </summary>
internal static class Repository
{
    public const string SolutionFileName = "subtrack-backend.slnx";

    public static string Root { get; } = FindRoot();

    /// <summary>Every project under <c>src/</c> and <c>tests/</c>, as a repo-relative path with forward slashes.</summary>
    public static IReadOnlyList<string> ProjectPaths { get; } = FindProjects();

    public static Layer GetLayer(string projectPath)
    {
        var segments = projectPath.Split('/');
        var name = GetProjectName(projectPath);

        return (segments[0], segments[1]) switch
        {
            ("src", "Common") => Layer.Common,
            ("src", "Models") => Layer.Models,
            ("src", "Infrastructure") => Layer.Infrastructure,
            ("src", "Grains" or "Services") when name.EndsWith(".Abstractions", StringComparison.Ordinal) => Layer.Abstractions,
            ("src", "Grains") => Layer.Grains,
            ("src", "Services") => Layer.Services,
            ("src", "ApplicationServices") => Layer.ApplicationServices,
            ("tests", "UnitTests") => Layer.UnitTests,
            ("tests", "IntegrationTests") => Layer.IntegrationTests,
            ("tests", "ArchitectureTests") => Layer.ArchitectureTests,
            _ => throw new InvalidOperationException(
                $"{projectPath} is not under a known layer folder. Move it to the right src/<Layer>/ or tests/<Kind>/ folder, " +
                "or add the new folder to the root CLAUDE.md and to Layer/Repository in SubTrack.ArchitectureTests."),
        };
    }

    public static string GetProjectName(string projectPath)
    {
        return Path.GetFileNameWithoutExtension(projectPath);
    }

    /// <summary>The repo-relative paths of the projects that <paramref name="projectPath"/> references.</summary>
    public static IReadOnlyList<string> GetProjectReferences(string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(ToFullPath(projectPath))!;

        return XDocument.Load(ToFullPath(projectPath))
            .Descendants("ProjectReference")
            .Select(reference => (string?)reference.Attribute("Include"))
            .OfType<string>()
            .Select(include => include.Replace('\\', '/').Replace('/', Path.DirectorySeparatorChar))
            .Select(include => ToRelativePath(Path.GetFullPath(Path.Combine(projectDirectory, include))))
            .ToList();
    }

    /// <summary>Maps each project path listed in the <c>.slnx</c> to the solution folder it is listed under.</summary>
    public static IReadOnlyDictionary<string, string> GetSolutionProjects()
    {
        return XDocument.Load(Path.Combine(Root, SolutionFileName))
            .Descendants("Project")
            .ToDictionary(
                project => ((string)project.Attribute("Path")!).Replace('\\', '/'),
                project => (string?)project.Parent?.Attribute("Name") ?? "/");
    }

    public static string ToFullPath(string relativePath)
    {
        return Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string ToRelativePath(string fullPath)
    {
        return Path.GetRelativePath(Root, fullPath).Replace(Path.DirectorySeparatorChar, '/');
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not find {SolutionFileName} in any parent of {AppContext.BaseDirectory}.");
    }

    private static List<string> FindProjects()
    {
        string[] buildOutputFolders = ["bin", "obj"];

        return new[] { "src", "tests" }
            .Select(folder => Path.Combine(Root, folder))
            .Where(Directory.Exists)
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.csproj", SearchOption.AllDirectories))
            .Select(ToRelativePath)
            .Where(path => !path.Split('/').Intersect(buildOutputFolders, StringComparer.OrdinalIgnoreCase).Any())
            .Order(StringComparer.Ordinal)
            .ToList();
    }
}
