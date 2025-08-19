using DotnetLegacyMigrator;
using Spectre.Console;

// Resolve repository root from the compiled executable location.
// The CLI binaries are under `src/Cli/bin/<Configuration>/net9.0/`.
// Moving five levels up reaches the repository root.
var repoRoot = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

// Examples were moved from `src/examples` to `/examples`.
// Check both locations to remain compatible with older layouts.
var examplesDir = Path.Combine(repoRoot, "examples");
if (!Directory.Exists(examplesDir))
{
    examplesDir = Path.Combine(repoRoot, "src", "examples");
}

var projects = Directory.Exists(examplesDir)
    ? Directory.GetDirectories(examplesDir)
        .Select(d => Path.GetFileName(d)!)
        .OrderBy(x => x)
        .ToList()
    : new List<string>();

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
