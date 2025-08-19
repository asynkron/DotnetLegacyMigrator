using DotnetLegacyMigrator;
using Spectre.Console;

var examplesDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "examples");
var projects = Directory.GetDirectories(examplesDir)
                        .Select(d => Path.GetFileName(d)!)
                        .OrderBy(x => x)
                        .ToList();

if (projects.Count == 0)
{
    AnsiConsole.MarkupLine("[red]No example projects found.[/]");
    return;
}

var choice = AnsiConsole.Prompt(
    new SelectionPrompt<string>()
        .Title("Select example project")
        .AddChoices(projects));

var slnPath = Directory.GetFiles(Path.Combine(examplesDir, choice), "*.sln").First();

var runner = new MigrationRunner();
await runner.RunAsync(slnPath);
