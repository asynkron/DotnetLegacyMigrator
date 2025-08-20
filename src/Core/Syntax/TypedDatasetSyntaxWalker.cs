using DotnetLegacyMigrator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotnetLegacyMigrator.Syntax;

public class TypedDatasetSyntaxWalker : CSharpSyntaxWalker
{
    private readonly ILogger<TypedDatasetSyntaxWalker> _logger;

    public TypedDatasetSyntaxWalker(ILogger<TypedDatasetSyntaxWalker>? logger = null)
    {
        _logger = logger ?? NullLogger<TypedDatasetSyntaxWalker>.Instance;
    }

    public List<DataContext> Contexts { get; } = new();

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        // Check for Typed DataSet class
        if (node.BaseList?.Types.Any(t => t.Type.ToString().Contains("DataSet")) == true)
        {
            var name = node.Identifier.ToString();
            var context = new DataContext
            {
                Name = name,
                Tables = ExtractTableNames(node).ToList(),
                StoredProcedures = new List<StoredProcedureMapping>() // No stored procedures in Typed Datasets
            };

            // Look for the corresponding TableAdapter
            var root = node.SyntaxTree.GetRoot();
            var tableAdapterNodes = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .Where(c =>
                    c.Identifier.ToString().Contains("TableAdapter", StringComparison.Ordinal));

            foreach (var tableAdapterNode in tableAdapterNodes)
            {
                var methodCommandInfos = ExtractMethodCommandInfo(tableAdapterNode);

                foreach (var m in methodCommandInfos)
                {
                    var returnType = ExtractDataSetTableName(tableAdapterNode);
                    var mapping = new StoredProcedureMapping()
                    {
                        ReturnType = returnType,

                        MethodName = (m.MethodName ?? string.Empty) + returnType,
                        StoredProcName = m.CommandText ?? string.Empty,
                        Parameters = m.Parameters?.Select(p => new ParameterMapping()
                        {
                            Name = p.Name,
                            Type = p.SqlDbType,
                            Direction = p.Direction,
                            Size = p.Size > 0 ? p.Size : null
                        }).ToList() ?? new List<ParameterMapping>(),

                    };

                    if (mapping.StoredProcName == "" || mapping.StoredProcName is null)
                    {
                        continue;
                    } 

                    context.StoredProcedures.Add(mapping);

                }
            }


            Contexts.Add(context);
        }

        base.VisitClassDeclaration(node);
    }

    /// <summary>
    /// Finds the InitAdapter method in a TableAdapter class,
    /// looks for “this._adapter” → tableMapping.DataSetTable = "XXX",
    /// and returns that XXX + "DataTable".
    /// </summary>
    private string ExtractDataSetTableName(ClassDeclarationSyntax adapterClass)
    {
        var init = adapterClass.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text == "InitAdapter");

        if (init?.Body == null)
            return adapterClass.Identifier.Text.Replace("TableAdapter", "");

        foreach (var stmt in init.Body.Statements.OfType<ExpressionStatementSyntax>())
        {
            if (stmt.Expression is AssignmentExpressionSyntax assign
                && assign.Left is MemberAccessExpressionSyntax left
                // match: tableMapping.DataSetTable
                && left.Name.Identifier.Text == "DataSetTable"
                // right side should be a string literal
                && assign.Right is LiteralExpressionSyntax lit
                && lit.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var tableName = lit.Token.ValueText;
                return tableName;
            }
        }

        // fallback
        return adapterClass.Identifier.Text.Replace("TableAdapter", "");
    }

    public List<MethodDeclarationSyntax> GetTableAdapterMethodsWithDataObjectMethod(ClassDeclarationSyntax tableAdapterNode)
    {
        // Initialize a list to store matching methods
        var methodsWithDataObjectMethod = new List<MethodDeclarationSyntax>();

        // Iterate through all method declarations in the TableAdapter node
        foreach (var method in tableAdapterNode.Members.OfType<MethodDeclarationSyntax>())
        {
            // Check if the method has the desired attribute
            if (method.AttributeLists
                .SelectMany(attrList => attrList.Attributes)
                .Any(attr => attr.ToString().Contains("System.ComponentModel.DataObjectMethod")))
            {
                methodsWithDataObjectMethod.Add(method);
            }
        }

        return methodsWithDataObjectMethod;
    }

    public List<MethodCommandInfo> ExtractMethodCommandInfo(ClassDeclarationSyntax tableAdapterNode)
    {
        var commandInfoList = new List<MethodCommandInfo>();

        // Print the name of the table adapter class (for debugging)
        _logger.LogDebug("Processing TableAdapter: {Adapter}", tableAdapterNode.Identifier);

        // Extract methods with DataObjectMethodAttribute
        var methodsWithAttribute = GetTableAdapterMethodsWithDataObjectMethod(tableAdapterNode);

        foreach (var method in methodsWithAttribute)
        {
            _logger.LogDebug("Processing Method: {Method}", method.Identifier);

            // Parse the body of the method to find the CommandCollection index
            var commandCollectionAccess = method.DescendantNodes()
                .OfType<ElementAccessExpressionSyntax>()
                .FirstOrDefault(expr => expr.Expression.ToString() == "this.CommandCollection");

            // Extract method parameters
            var parameters = method.ParameterList.Parameters
                .Where(p => p.Identifier.ToString() != "dataTable")
                .Select(p => new SqlParameterInfo
                {
                    Name = p.Identifier.ToString(),
                    SqlDbType = p.Type?.ToString() ?? "Unknown",
                    Direction = p.Modifiers.Any(m => m.IsKind(SyntaxKind.OutKeyword)) ? "Output" :
                                p.Modifiers.Any(m => m.IsKind(SyntaxKind.RefKeyword)) ? "InputOutput" : "Input"
                }).ToList();

            var methodName = method.Identifier.ToString();

            if (commandCollectionAccess?.ArgumentList.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax commandIndexLiteral &&
                int.TryParse(commandIndexLiteral.Token.ValueText, out var commandIndex))
            {
                _logger.LogDebug("Found CommandCollection index: {Index}", commandIndex);

                // Extract command info using the index
                var commandInfo = ExtractCommandInfoFromInitCommandCollection(tableAdapterNode, commandIndex);
                if (commandInfo != null)
                {
                    commandInfo.MethodName = methodName;
                    commandInfo.ParameterIndex = commandIndex; // CommandCollection index
                    commandInfo.Parameters = parameters; // Add method parameters
                    commandInfoList.Add(commandInfo);
                }
            }
            else
            {
                _logger.LogDebug("No CommandCollection index found for this method.");
            }
        }

        return commandInfoList;
    }




    public MethodCommandInfo? ExtractCommandInfoFromInitCommandCollection(ClassDeclarationSyntax tableAdapterNode, int parameterIndex)
    {
        var initCommandCollection = tableAdapterNode.Members
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.ToString() == "InitCommandCollection");

        if (initCommandCollection == null) return null;

        // Locate the command initialization statement
        var commandArrayElement = initCommandCollection.DescendantNodes()
            .OfType<ExpressionStatementSyntax>()
            .FirstOrDefault(expr => expr.ToString().Contains($"_commandCollection[{parameterIndex}] ="));

        if (commandArrayElement == null) return null;

        // Find the CommandText assignment
        var commandTextExpression = initCommandCollection.DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .FirstOrDefault(ae => ae.Left.ToString().Contains($"_commandCollection[{parameterIndex}].CommandText"));

        // Extract CommandText
        string? commandText = null;
        if (commandTextExpression?.Right is LiteralExpressionSyntax literalExpression)
        {
            commandText = literalExpression.Token.ValueText;
        }

        return new MethodCommandInfo
        {
            CommandText = commandText,

        };
    }



    private IEnumerable<TableMapping> ExtractTableNames(ClassDeclarationSyntax datasetClass)
    {
        // Look for nested classes that represent tables
        foreach (var member in datasetClass.Members.OfType<ClassDeclarationSyntax>())
        {
            if (member.BaseList?.Types.Any(t => t.Type.ToString().Contains("TypedTableBase") ||
                                             t.Type.ToString().Contains("DataTable")) == true)
            {
                if (member.Identifier.ToString() == "DataTable1")
                    continue;

                yield return new TableMapping
                {
                    Name = member.Identifier.ToString().Replace("DataTable",""),
                    EntityType = member.Identifier.ToString().Replace("DataTable", "")
                };
            }
        }
    }


    public class MethodCommandInfo
    {
        public string? MethodName { get; set; }
        public int ParameterIndex { get; set; }
        public string? CommandText { get; set; }
        public List<SqlParameterInfo>? Parameters { get; set; }
    }

    public class SqlParameterInfo
    {
        public required string Name { get; set; }
        public required string SqlDbType { get; set; }
        public int Size { get; set; }
        public required string Direction { get; set; }
    }
}