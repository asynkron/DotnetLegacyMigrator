using System;
using System.Collections.Generic;
using DotnetLegacyMigrator.Rewriters;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Translation.Tests;

public class LoggingTests
{
    private const string SampleClass = "public class Skipped { public int Id { get; set; } }";

    [Fact]
    public void CtorInjectRewriter_Logs_Skipping()
    {
        var tree = CSharpSyntaxTree.ParseText(SampleClass);
        var logger = new TestLogger<CtorInjectRewriter>();
        var rewriter = new CtorInjectRewriter(logger);
        rewriter.Visit(tree.GetRoot());

        Assert.Contains(logger.Messages, m => m.Contains("Skipping type Skipped"));
    }

    [Fact]
    public void CtorInjectRewriter_DisabledLogging_CapturesNoMessages()
    {
        var tree = CSharpSyntaxTree.ParseText(SampleClass);
        var logger = new TestLogger<CtorInjectRewriter>(enabled: false);
        var rewriter = new CtorInjectRewriter(logger);
        rewriter.Visit(tree.GetRoot());

        Assert.Empty(logger.Messages);
    }

    private class TestLogger<T> : ILogger<T>
    {
        private readonly bool _enabled;
        public List<string> Messages { get; } = new();

        public TestLogger(bool enabled = true)
        {
            _enabled = enabled;
        }

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => _enabled;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (_enabled)
            {
                Messages.Add(formatter(state, exception));
            }
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new NullScope();
            public void Dispose() { }
        }
    }
}
