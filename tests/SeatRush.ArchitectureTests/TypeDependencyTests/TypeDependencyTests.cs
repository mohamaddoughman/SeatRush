using System.Reflection;
using NetArchTest.Rules;

namespace SeatRush.ArchitectureTests.TypeDependencyTests;

/// <summary>
/// Module-boundary rules checked against the compiled code: which types each type actually uses.
/// </summary>
/// <remarks>
/// Why: these catch dependencies the .csproj doesn't show, e.g. a Domain type using an ASP.NET Core type
/// that arrived through some other package. They cannot see references that are declared but unused;
/// the project-reference tests cover that.
/// </remarks>
public sealed class TypeDependencyTests
{
    private const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";
    private const string AspNetCore = "Microsoft.AspNetCore";

    public static TheoryData<string> Modules => new(RepositoryLayout.ModuleNames);

    [Theory]
    [MemberData(nameof(Modules))]
    public void Module_uses_other_modules_only_through_their_contracts(string module)
    {
        var otherModulesInternals = RepositoryLayout.ModuleNames
            .Where(other => other != module)
            .SelectMany(other => new[]
            {
                RepositoryLayout.ProjectName(other, RepositoryLayout.Domain),
                RepositoryLayout.ProjectName(other, RepositoryLayout.Application),
                RepositoryLayout.ProjectName(other, RepositoryLayout.Infrastructure),
            })
            .ToArray();

        var result = Types.InAssemblies(ModuleAssemblies(module))
            .ShouldNot()
            .HaveDependencyOnAny(otherModulesInternals)
            .GetResult();

        AssertNoFailingTypes(result);
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Domain_does_not_use_outer_layers_or_frameworks(string module)
    {
        var result = Types.InAssembly(RepositoryLayout.LoadAssembly(module, RepositoryLayout.Domain))
            .ShouldNot()
            .HaveDependencyOnAny(
                RepositoryLayout.ProjectName(module, RepositoryLayout.Application),
                RepositoryLayout.ProjectName(module, RepositoryLayout.Infrastructure),
                EntityFrameworkCore,
                AspNetCore)
            .GetResult();

        AssertNoFailingTypes(result);
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Contracts_do_not_expose_module_internals_or_frameworks(string module)
    {
        var result = Types.InAssembly(RepositoryLayout.LoadAssembly(module, RepositoryLayout.Contracts))
            .ShouldNot()
            .HaveDependencyOnAny(
                RepositoryLayout.ProjectName(module, RepositoryLayout.Domain),
                RepositoryLayout.ProjectName(module, RepositoryLayout.Application),
                RepositoryLayout.ProjectName(module, RepositoryLayout.Infrastructure),
                EntityFrameworkCore,
                AspNetCore)
            .GetResult();

        AssertNoFailingTypes(result);
    }

    [Theory]
    [MemberData(nameof(Modules))]
    public void Application_does_not_use_infrastructure(string module)
    {
        var result = Types.InAssembly(RepositoryLayout.LoadAssembly(module, RepositoryLayout.Application))
            .ShouldNot()
            .HaveDependencyOnAny(RepositoryLayout.ProjectName(module, RepositoryLayout.Infrastructure))
            .GetResult();

        AssertNoFailingTypes(result);
    }

    [Fact]
    public void Shared_does_not_use_any_module_or_the_api()
    {
        var forbidden = RepositoryLayout.ModuleNames
            .Select(module => $"SeatRush.{module}.")
            .Append("SeatRush.Api")
            .ToArray();

        var result = Types.InAssembly(Assembly.Load(new AssemblyName("SeatRush.Shared")))
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        AssertNoFailingTypes(result);
    }

    private static Assembly[] ModuleAssemblies(string module) =>
        RepositoryLayout.Layers.Select(layer => RepositoryLayout.LoadAssembly(module, layer)).ToArray();

    private static void AssertNoFailingTypes(NetArchTest.Rules.TestResult result) =>
        Assert.Empty(result.FailingTypes?.Select(type => type.FullName) ?? []);
}
