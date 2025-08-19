using DotnetLegacyMigrator.Models;
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
    /// Loads the provided solution and collects metadata for all supported legacy models.
    /// </summary>
    public async Task RunAsync(string solutionPath)
    {
        if (!File.Exists(solutionPath))
        {
            Console.WriteLine($"Could not find the solution: {solutionPath}");
            return;
        }

        Console.WriteLine($"Loading solution '{solutionPath}'");
        // MetadataCollector internally runs all known syntax walkers and parsers
        var (allContexts, allEntities) = await MetadataCollector.CollectAsync(solutionPath);

        // If nothing was discovered we should surface a helpful message
        if (!allEntities.Any() && !allContexts.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No supported legacy models were found in the solution.[/]");
            return;
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
