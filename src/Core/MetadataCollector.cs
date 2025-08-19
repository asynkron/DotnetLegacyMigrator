using DotnetLegacyMigrator.Models;
using DotnetLegacyMigrator.Syntax;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

namespace DotnetLegacyMigrator;

/// <summary>
/// Helper that uses the existing syntax walkers to produce metadata
/// for DataContexts and Entities without modifying the source projects.
/// </summary>
public static class MetadataCollector
{
    public static async Task<(List<DataContext> Contexts, List<Entity> Entities)> CollectAsync(string solutionPath)
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
        var _ = typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions);

        using var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionPath);

        var contexts = new List<DataContext>();
        var entities = new List<Entity>();

        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                var root = await document.GetSyntaxRootAsync();
                if (root == null) continue;

                var linqCtx = new LinqToSqlContextSyntaxWalker();
                var linqEntities = new LinqToSqlEntitySyntaxWalker();
                var datasetCtx = new TypedDatasetSyntaxWalker();
                var datasetEntities = new TypedDatasetEntitySyntaxWalker();

                linqCtx.Visit(root);
                linqEntities.Visit(root);
                datasetCtx.Visit(root);
                datasetEntities.Visit(root);

                contexts.AddRange(linqCtx.Contexts);
                contexts.AddRange(datasetCtx.Contexts);
                entities.AddRange(linqEntities.Entities);
                entities.AddRange(datasetEntities.Entities);
            }

            // NHibernate mapping files live outside of C# documents
            var projectDir = Path.GetDirectoryName(project.FilePath);
            if (projectDir != null)
            {
                var hbmFiles = Directory.GetFiles(projectDir, "*.hbm.xml", SearchOption.AllDirectories);
                if (hbmFiles.Length > 0)
                {
                    var (ctx, nhEntities) = NHibernateHbmParser.ParseFiles(hbmFiles);
                    contexts.Add(ctx);
                    entities.AddRange(nhEntities);
                }
            }
        }

        return (contexts, entities);
    }
}
