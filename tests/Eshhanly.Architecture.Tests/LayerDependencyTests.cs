using System.Reflection;
using NetArchTest.Rules;

namespace Eshhanly.Architecture.Tests;

/// <summary>Guards the Clean Architecture rule: dependencies only point inward (Web → Infrastructure → Application → Domain).</summary>
public class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(Domain.Common.Entity).Assembly;
    private static readonly Assembly Application = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_depends_on_no_other_layer_or_framework()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Eshhanly.Application",
                "Eshhanly.Infrastructure",
                "Eshhanly.Web",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "MediatR")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailingTypes(result));
    }

    [Fact]
    public void Application_does_not_depend_on_Infrastructure_or_Web()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny("Eshhanly.Infrastructure", "Eshhanly.Web", "Npgsql", "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailingTypes(result));
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_Web()
    {
        var result = Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOn("Eshhanly.Web")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailingTypes(result));
    }

    private static string FailingTypes(TestResult result) =>
        string.Join(", ", result.FailingTypeNames ?? []);
}
