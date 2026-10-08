using System.Xml.Linq;

namespace SeatRush.ArchitectureTests;

/// <summary>
/// What a .csproj declares: its SDK and its project, package and framework references.
/// </summary>
internal sealed record ProjectFile(
    string Name,
    string Sdk,
    IReadOnlyList<string> ProjectReferences,
    IReadOnlyList<string> PackageReferences,
    IReadOnlyList<string> FrameworkReferences)
{
    /// <summary>The module this project belongs to, or null for Api and Shared.</summary>
    public string? Module { get; private init; }

    /// <summary>The layer (Contracts, Domain, Application, Infrastructure), or null for Api and Shared.</summary>
    public string? Layer { get; private init; }

    public static ProjectFile Load(string path)
    {
        var root = XDocument.Load(path).Root
            ?? throw new InvalidOperationException($"{path} has no root element.");

        var name = Path.GetFileNameWithoutExtension(path);
        var (module, layer) = ParseModuleAndLayer(name);

        return new ProjectFile(
            name,
            (string?)root.Attribute("Sdk") ?? string.Empty,
            Includes(root, "ProjectReference").Select(ProjectNameFromPath).ToList(),
            Includes(root, "PackageReference").ToList(),
            Includes(root, "FrameworkReference").ToList())
        {
            Module = module,
            Layer = layer,
        };
    }

    public override string ToString() => Name;

    private static IEnumerable<string> Includes(XElement root, string itemType) =>
        root.Descendants(itemType)
            .Select(item => (string?)item.Attribute("Include"))
            .OfType<string>();

    // "..\SeatRush.Booking.Domain\SeatRush.Booking.Domain.csproj" -> "SeatRush.Booking.Domain"
    private static string ProjectNameFromPath(string include) =>
        Path.GetFileNameWithoutExtension(include.Replace('\\', '/').Split('/')[^1]);

    // "SeatRush.Booking.Domain" -> ("Booking", "Domain"); anything else -> (null, null)
    private static (string? Module, string? Layer) ParseModuleAndLayer(string projectName) =>
        projectName.Split('.') is ["SeatRush", var module, var layer] && RepositoryLayout.Layers.Contains(layer)
            ? (module, layer)
            : (null, null);
}
