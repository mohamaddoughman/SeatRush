namespace SeatRush.ArchitectureTests.ProjectReferenceTests;

/// <summary>
/// Module-boundary rules checked against what each .csproj <em>declares</em>.
/// </summary>
/// <remarks>
/// Why: these catch a forbidden reference as soon as it is added, even if no code uses it yet
/// (the type-dependency tests cannot see an unused reference, because the compiler drops it).
/// They cannot see what a reference brings in transitively; the type-dependency tests cover that.
/// </remarks>
public sealed class ProjectReferenceTests
{
    private static readonly string[] ForbiddenDomainPackagePrefixes = ["Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore."];

    public static TheoryData<string> ModuleProjects => new(RepositoryLayout.ModuleProjects.Select(p => p.Name));

    public static TheoryData<string> DomainProjects => ProjectsInLayer(RepositoryLayout.Domain);

    public static TheoryData<string> ContractsProjects => ProjectsInLayer(RepositoryLayout.Contracts);

    public static TheoryData<string> ApplicationProjects => ProjectsInLayer(RepositoryLayout.Application);

    [Fact]
    public void Every_module_has_exactly_the_four_layer_projects()
    {
        var expected = RepositoryLayout.ModuleNames
            .SelectMany(module => RepositoryLayout.Layers.Select(layer => RepositoryLayout.ProjectName(module, layer)))
            .Order(StringComparer.Ordinal);

        var actual = RepositoryLayout.SourceProjects
            .Where(p => p.Name.Split('.') is ["SeatRush", var module, ..] && RepositoryLayout.ModuleNames.Contains(module))
            .Select(p => p.Name)
            .Order(StringComparer.Ordinal);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(ModuleProjects))]
    public void Module_references_other_modules_only_through_their_contracts(string projectName)
    {
        var project = RepositoryLayout.Project(projectName);

        var violations = project.ProjectReferences
            .Select(RepositoryLayout.Project)
            .Where(target => target.Module is not null
                && target.Module != project.Module
                && target.Layer != RepositoryLayout.Contracts)
            .Select(target => target.Name);

        Assert.Empty(violations);
    }

    [Theory]
    [MemberData(nameof(DomainProjects))]
    public void Domain_does_not_reference_outer_layers_or_frameworks(string projectName)
    {
        var project = RepositoryLayout.Project(projectName);

        var violations = new List<string>();

        violations.AddRange(project.ProjectReferences
            .Select(RepositoryLayout.Project)
            .Where(target => target.Layer is RepositoryLayout.Application or RepositoryLayout.Infrastructure)
            .Select(target => $"project reference: {target.Name}"));

        violations.AddRange(project.PackageReferences
            .Where(package => ForbiddenDomainPackagePrefixes.Any(prefix => package.StartsWith(prefix, StringComparison.Ordinal)))
            .Select(package => $"package reference: {package}"));

        violations.AddRange(project.FrameworkReferences
            .Where(framework => framework == "Microsoft.AspNetCore.App")
            .Select(framework => $"framework reference: {framework}"));

        if (project.Sdk != "Microsoft.NET.Sdk")
        {
            violations.Add($"SDK: {project.Sdk} (must be Microsoft.NET.Sdk)");
        }

        Assert.Empty(violations);
    }

    [Theory]
    [MemberData(nameof(ContractsProjects))]
    public void Contracts_reference_only_shared(string projectName)
    {
        var project = RepositoryLayout.Project(projectName);

        var violations = project.ProjectReferences
            .Where(reference => reference != "SeatRush.Shared")
            .Select(reference => $"project reference: {reference}")
            .Concat(project.PackageReferences.Select(package => $"package reference: {package}"));

        Assert.Empty(violations);
    }

    [Theory]
    [MemberData(nameof(ApplicationProjects))]
    public void Application_does_not_reference_infrastructure(string projectName)
    {
        var project = RepositoryLayout.Project(projectName);

        var violations = project.ProjectReferences
            .Select(RepositoryLayout.Project)
            .Where(target => target.Layer == RepositoryLayout.Infrastructure)
            .Select(target => target.Name);

        Assert.Empty(violations);
    }

    [Fact]
    public void Shared_references_no_project()
    {
        Assert.Empty(RepositoryLayout.Project("SeatRush.Shared").ProjectReferences);
    }

    [Fact]
    public void Nothing_references_the_api()
    {
        var violations = RepositoryLayout.SourceProjects
            .Where(project => project.ProjectReferences.Contains("SeatRush.Api"))
            .Select(project => project.Name);

        Assert.Empty(violations);
    }

    private static TheoryData<string> ProjectsInLayer(string layer) =>
        new(RepositoryLayout.ModuleProjects.Where(p => p.Layer == layer).Select(p => p.Name));
}
