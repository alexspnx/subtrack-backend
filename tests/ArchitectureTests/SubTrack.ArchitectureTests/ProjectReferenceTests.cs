namespace SubTrack.ArchitectureTests;

/// <summary>
/// Enforces the dependency direction from the root CLAUDE.md:
/// Common -> Models -> Infrastructure / *.Abstractions -> Grains / Services -> ApplicationServices.
/// Common projects may also reference other Common projects.
/// If a rule here changes, update the CLAUDE.md files in the same change (see the sync-docs skill).
/// </summary>
public class ProjectReferenceTests
{
    private static readonly Dictionary<Layer, Layer[]> AllowedReferences = new()
    {
        [Layer.Common] = [Layer.Common],
        [Layer.Models] = [Layer.Common],
        [Layer.Infrastructure] = [Layer.Common, Layer.Models],
        [Layer.Abstractions] = [Layer.Common, Layer.Models],
        [Layer.Grains] = [Layer.Common, Layer.Models, Layer.Infrastructure, Layer.Abstractions],
        [Layer.Services] = [Layer.Common, Layer.Models, Layer.Infrastructure, Layer.Abstractions],
        [Layer.ApplicationServices] =
            [Layer.Common, Layer.Models, Layer.Infrastructure, Layer.Abstractions, Layer.Grains, Layer.Services],
        // Plus the one project under test, checked separately below.
        [Layer.UnitTests] = [Layer.Common, Layer.Models, Layer.Abstractions],
        [Layer.IntegrationTests] =
            [Layer.Common, Layer.Models, Layer.Infrastructure, Layer.Abstractions, Layer.Grains, Layer.Services, Layer.ApplicationServices],
        [Layer.ArchitectureTests] = [],
    };

    public static TheoryData<string> Projects()
    {
        var data = new TheoryData<string>();
        foreach (var projectPath in Repository.ProjectPaths)
        {
            data.Add(projectPath);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Project_is_in_a_known_layer_folder(string projectPath)
    {
        var exception = Record.Exception(() => Repository.GetLayer(projectPath));

        Assert.Null(exception);
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Project_only_references_layers_below_it(string projectPath)
    {
        var layer = Repository.GetLayer(projectPath);
        var allowed = AllowedReferences[layer];
        var testedProject = layer == Layer.UnitTests ? FindProjectUnderTest(projectPath) : null;

        var violations = Repository.GetProjectReferences(projectPath)
            .Where(reference => reference != testedProject)
            .Select(reference => (Path: reference, Layer: Repository.GetLayer(reference)))
            .Where(reference => !allowed.Contains(reference.Layer))
            .Select(reference => $"{reference.Path} ({reference.Layer})")
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"{projectPath} ({layer}) may only reference [{string.Join(", ", allowed)}], but references: {string.Join("; ", violations)}");
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void Unit_test_project_references_the_project_it_is_named_after(string projectPath)
    {
        if (Repository.GetLayer(projectPath) != Layer.UnitTests)
        {
            return;
        }

        var testedProject = FindProjectUnderTest(projectPath);

        Assert.True(
            testedProject is not null,
            $"{projectPath} must be named <ProjectName>.UnitTests after an existing project in src/Grains or src/Services.");
        Assert.Contains(testedProject, Repository.GetProjectReferences(projectPath));
    }

    private static string? FindProjectUnderTest(string unitTestProjectPath)
    {
        const string suffix = ".UnitTests";
        var name = Repository.GetProjectName(unitTestProjectPath);
        if (!name.EndsWith(suffix, StringComparison.Ordinal))
        {
            return null;
        }

        var testedName = name[..^suffix.Length];

        return Repository.ProjectPaths.SingleOrDefault(path =>
            Repository.GetProjectName(path) == testedName &&
            Repository.GetLayer(path) is Layer.Grains or Layer.Services);
    }
}
