namespace SubTrack.ArchitectureTests;

/// <summary>
/// Enforces that the folder layout in the .slnx matches the folder layout on disk (root CLAUDE.md, "Adding a new project").
/// </summary>
public class SolutionFileTests
{
    public static TheoryData<string> Projects()
    {
        return ProjectReferenceTests.Projects();
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Project_lives_in_a_folder_named_after_it(string projectPath)
    {
        var folderName = Path.GetFileName(Path.GetDirectoryName(projectPath));

        Assert.Equal(Repository.GetProjectName(projectPath), folderName);
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Project_is_listed_in_the_solution_under_the_matching_folder(string projectPath)
    {
        var solutionProjects = Repository.GetSolutionProjects();
        var projectDirectory = Path.GetDirectoryName(projectPath)!.Replace('\\', '/');
        var expectedFolder = "/" + Path.GetDirectoryName(projectDirectory)!.Replace('\\', '/') + "/";

        Assert.True(
            solutionProjects.TryGetValue(projectPath, out var actualFolder),
            $"{projectPath} exists on disk but is not listed in {Repository.SolutionFileName}.");
        Assert.Equal(expectedFolder, actualFolder);
    }

    [Fact]
    public void Every_project_in_the_solution_exists_on_disk()
    {
        var missing = Repository.GetSolutionProjects().Keys
            .Where(path => !File.Exists(Repository.ToFullPath(path)))
            .ToList();

        Assert.True(missing.Count == 0, $"Listed in {Repository.SolutionFileName} but missing on disk: {string.Join("; ", missing)}");
    }
}
