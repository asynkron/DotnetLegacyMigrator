using DotnetLegacyMigrator.Models;
using DotnetLegacyMigrator.Syntax;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;

namespace DotnetLegacyMigrator;

/// <summary>
/// Coordinates reading a solution and extracting metadata using the syntax walkers.
/// This is a greatly simplified form of the previous top-level program logic.
/// </summary>
public class MigrationRunner
{
    /// <summary>
    /// Loads the provided solution and runs the metadata walkers.
    /// </summary>
    public async Task RunAsync(string solutionPath)
    {
        if (!MSBuildLocator.IsRegistered)
        {
            // Register MSBuild only once to avoid exceptions on subsequent invocations
            MSBuildLocator.RegisterDefaults();
        }

        var _ = typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions);

        if (!File.Exists(solutionPath))
        {
            Console.WriteLine($"Could not find the solution: {solutionPath}");
            return;
        }

        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, e) => Console.WriteLine(e.Diagnostic.Message);

        Console.WriteLine($"Loading solution '{solutionPath}'");
        var solution = await workspace.OpenSolutionAsync(solutionPath);
        var currentSolution = solution;

        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                var syntaxRoot = await document.GetSyntaxRootAsync();
                if (syntaxRoot == null) continue;

                var nsWalker = new NamespaceWalker();
                var entitySyntaxWalker = new TypedDatasetEntitySyntaxWalker();
                var contextSyntaxWalker = new TypedDatasetSyntaxWalker();

                nsWalker.Visit(syntaxRoot);
                entitySyntaxWalker.Visit(syntaxRoot);
                contextSyntaxWalker.Visit(syntaxRoot);

                if (entitySyntaxWalker.Entities.Any())
                {
                    Console.WriteLine($"{entitySyntaxWalker.Entities.Count} entities discovered in {document.Name}.");
                }
            }
        }
    }
}
