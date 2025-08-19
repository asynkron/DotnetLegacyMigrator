using DotnetLegacyMigrator.Models;
using DotnetLegacyMigrator.Syntax;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using Spectre.Console;
using System.Text.RegularExpressions;

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

        var allEntities = new List<Entity>();
        var allContexts = new List<DataContext>();

        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                var syntaxRoot = await document.GetSyntaxRootAsync();
                if (syntaxRoot == null)
                    continue;

                var nsWalker = new NamespaceWalker();
                var entitySyntaxWalker = new TypedDatasetEntitySyntaxWalker();
                var contextSyntaxWalker = new TypedDatasetSyntaxWalker();

                nsWalker.Visit(syntaxRoot);
                entitySyntaxWalker.Visit(syntaxRoot);
                contextSyntaxWalker.Visit(syntaxRoot);

                if (entitySyntaxWalker.Entities.Any())
                {
                    Console.WriteLine($"{entitySyntaxWalker.Entities.Count} entities discovered in {document.Name}.");
                    allEntities.AddRange(entitySyntaxWalker.Entities);
                }

                if (contextSyntaxWalker.Contexts.Any())
                    allContexts.AddRange(contextSyntaxWalker.Contexts);
            }
        }

        if (allEntities.Any())
        {
            var entityCode = CodeGenerator.GenerateEntities(allEntities);
            AnsiConsole.Write(new Rule("Generated Entities"));
            AnsiConsole.MarkupLine(HighlightCSharp(entityCode));

            var configCode = CodeGenerator.GenerateEntityConfigurations(allEntities);
            AnsiConsole.Write(new Rule("Entity Configurations"));
            AnsiConsole.MarkupLine(HighlightCSharp(configCode));
        }

        foreach (var ctx in allContexts)
        {
            var ctxCode = CodeGenerator.GenerateDataContext(ctx);
            AnsiConsole.Write(new Rule($"Data Context: {ctx.Name}"));
            AnsiConsole.MarkupLine(HighlightCSharp(ctxCode));
        }
    }

    private static string HighlightCSharp(string code)
    {
        var escaped = Markup.Escape(code);
        var keywords = new[]
        {
            "using", "namespace", "public", "class", "void", "return",
            "new", "var", "int", "string", "async", "await", "List", "DbSet"
        };

        foreach (var keyword in keywords)
            escaped = Regex.Replace(escaped, $"\\b{keyword}\\b", $"[yellow]{keyword}[/]", RegexOptions.Multiline);

        return escaped;
    }
}
