using System.Reflection;

namespace SeatRush.ArchitectureTests;

/// <summary>
/// Discovers modules and projects from disk, so a new module is covered by every rule
/// without anyone remembering to update the tests.
/// </summary>
internal static class RepositoryLayout
{
    public const string Contracts = "Contracts";
    public const string Domain = "Domain";
    public const string Application = "Application";
    public const string Infrastructure = "Infrastructure";

    public static readonly string[] Layers = [Contracts, Domain, Application, Infrastructure];

    public static string Root { get; } = FindRootDirectory();

    /// <summary>Module names, e.g. "Booking", taken from the folders under src/Modules.</summary>
    public static IReadOnlyList<string> ModuleNames { get; } =
        Directory.GetDirectories(Path.Combine(Root, "src", "Modules"))
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>Every production project under src/. Tests and aspire/ projects are deliberately excluded.</summary>
    public static IReadOnlyList<ProjectFile> SourceProjects { get; } =
        Directory.GetFiles(Path.Combine(Root, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(ProjectFile.Load)
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToList();

    public static IEnumerable<ProjectFile> ModuleProjects => SourceProjects.Where(p => p.Module is not null);

    public static ProjectFile Project(string name) => SourceProjects.Single(p => p.Name == name);

    public static string ProjectName(string module, string layer) => $"SeatRush.{module}.{layer}";

    public static Assembly LoadAssembly(string module, string layer) =>
        Assembly.Load(new AssemblyName(ProjectName(module, layer)));

    private static string FindRootDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SeatRush.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not find SeatRush.slnx above the test output directory.");
    }
}
