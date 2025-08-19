using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotnetLegacyMigrator.Rewriters;

/// <summary>
/// Applies all migration rewriters to a document and returns the transformed
/// source code without modifying the original project.
/// </summary>
public static class RewriterPipeline
{
    /// <summary>
    /// Runs the rewriter pipeline against the provided document and returns the
    /// updated source code as a string. The document within the workspace is left
    /// untouched so callers can decide how to persist the changes.
    /// </summary>
    /// <param name="document">The document to rewrite.</param>
    /// <param name="loggerFactory">Optional logger factory for diagnostics.</param>
    /// <returns>The rewritten source code, or an empty string if the document has no root.</returns>
    public static async Task<string> ApplyAsync(Document document, ILoggerFactory? loggerFactory = null)
    {
        var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
        if (root is null)
            return string.Empty;

        loggerFactory ??= NullLoggerFactory.Instance;

        // Apply each rewriter in sequence. Each rewriter returns a new syntax tree
        // without altering the original document.
        root = new NewRewriter(loggerFactory.CreateLogger<NewRewriter>()).Visit(root);
        root = new ResolveRewriter(loggerFactory.CreateLogger<ResolveRewriter>()).Visit(root);
        root = new CtorInjectRewriter(loggerFactory.CreateLogger<CtorInjectRewriter>()).Visit(root);

        // Normalize whitespace so the output is clean and deterministic for tests.
        return root.NormalizeWhitespace().ToFullString();
    }
}

