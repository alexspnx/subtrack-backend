namespace SubTrack.ArchitectureTests;

/// <summary>
/// The layers described in the root CLAUDE.md. Projects named <c>*.Abstractions</c> under <c>src/Grains</c> or
/// <c>src/Services</c> form their own layer, because they sit lower in the dependency graph than their implementations.
/// </summary>
public enum Layer
{
    Common,
    Models,
    Infrastructure,
    Abstractions,
    Grains,
    Services,
    ApplicationServices,
    UnitTests,
    IntegrationTests,
    ArchitectureTests,
}
