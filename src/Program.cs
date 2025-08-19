using LinqToSqlMetadataExtractor;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Options;
using RoslynToy;
using System.Text.RegularExpressions;
using System.Text;
using TypedDatasetMetadataExtractor;




MSBuildLocator.RegisterDefaults();
var _ = typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions);

var solutionPath = @"pathtosolutionfile.sln"; //fix this
if (!File.Exists(solutionPath))
{
    Console.WriteLine($"Could not find the solution: {solutionPath}");
    return;
}

using var workspace = MSBuildWorkspace.Create();
// Attach an event handler to capture any workspace failed events
workspace.WorkspaceFailed += (_, e) =>
{
    Console.WriteLine(e.Diagnostic.Message);
};

Console.WriteLine($"Loading solution '{solutionPath}'");

// Load the solution file
var solution = await workspace.OpenSolutionAsync(solutionPath);

// Track the current solution
var currentSolution = solution;

// Process each project in the solution
foreach (var p in solution.Projects)
{
    var project = p!;
    // Process each document in the project
    foreach (var document in project.Documents)
    {
        //    Console.WriteLine($"Processing {document.FilePath}...");

        var syntaxRoot = await document.GetSyntaxRootAsync();
        if (syntaxRoot == null) continue; // Skip non-C# documents

        var nsWalker = new NamespaceWalker();
        //var entitySyntaxWalker = new LinqToSqlEntitySyntaxWalker();
        //var contextSyntaxWalker = new LinqToSqlContextSyntaxWalker();
        var entitySyntaxWalker = new TypedDatasetEntitySyntaxWalker();
        var contextSyntaxWalker = new TypedDatasetSyntaxWalker();
        var newSyntaxRoot = syntaxRoot;
        // Process each project in the solution
        //newSyntaxRoot = new NewRewriter().Visit(newSyntaxRoot);
        //newSyntaxRoot = new ResolveRewriter().Visit(newSyntaxRoot);
        //newSyntaxRoot = new CtorInjectRewriter().Visit(newSyntaxRoot);


        var path = Path.GetDirectoryName(document.FilePath);


        nsWalker.Visit(syntaxRoot);
        entitySyntaxWalker.Visit(syntaxRoot);
        contextSyntaxWalker.Visit(syntaxRoot);


        var xx = GenerateEfCoreEntities(entitySyntaxWalker.Entities, entitySyntaxWalker.StoredProcedureResults, nsWalker.Namespace);
        if (entitySyntaxWalker.Entities.Any())
        {
            Console.WriteLine(xx);
            var f = Path.GetFileNameWithoutExtension(document.FilePath).Replace(".designer", "", StringComparison.InvariantCultureIgnoreCase);
            var entityPath = Path.Combine(path, f + "EfEntities.cs");
            var entitiesDocument = project.AddDocument(entityPath, xx);
            currentSolution = entitiesDocument.Project.Solution;
            project = currentSolution.GetProject(project.Id);
        }


        //TODO: why did I do it like this?, there is always just 1 context per file....
        if (contextSyntaxWalker.Contexts.Any())
        {
            foreach (var context in contextSyntaxWalker.Contexts)
            {
                var contextPath = Path.Combine(path, context.Name + "Ef.cs");
                var yy = GenerateDbContext(context, nsWalker.Namespace, entitySyntaxWalker.StoredProcedureResults);
                Console.WriteLine(yy);

                var ctxDocument = project.AddDocument(contextPath, yy);
                currentSolution = ctxDocument.Project.Solution;
                project = currentSolution.GetProject(project.Id);
            }
        }





        if (newSyntaxRoot != syntaxRoot)
        {
            //    // Format the new syntax tree to fix whitespace, indentation, etc.


            //    // Set the IndentationSize to 4
            var options =
                workspace.Options
                    .WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, true)
                    .WithChangedOption(FormattingOptions.TabSize, LanguageNames.CSharp, 4);

            var formattedSyntaxRoot = Formatter.Format(newSyntaxRoot, workspace, options);
            Console.WriteLine(formattedSyntaxRoot.GetText());
            Console.WriteLine(newSyntaxRoot.GetText());

            // New syntax tree is created after modifications, this needs to be applied back to the document
            var newDocument = document.WithSyntaxRoot(formattedSyntaxRoot);
            // The document has been changed, so apply the change to the current solution
            currentSolution = currentSolution.WithDocumentSyntaxRoot(document.Id, formattedSyntaxRoot);
        }
    }
}

// After processing all documents, apply the changes to the original workspace
if (workspace.TryApplyChanges(currentSolution))
{
    Console.WriteLine("Changes applied successfully!");
}
else
{
    Console.WriteLine("Failed to apply changes.");
}

// If you want to persist changes back to disk, you need to manually handle file writes here.
// For example, you could iterate through all documents in the modified solution, get their text, and write it back to their file paths.

Console.WriteLine("Solution processing completed.");

static string GenerateEfCoreEntities(List<Entity> entities, List<StoredProcedureResult> storedProcedureResults, string nsWalkerNamespace)
{
    var efCoreEntities = new StringBuilder();

    // Add necessary usings
    efCoreEntities.AppendLine("using System;");
    efCoreEntities.AppendLine("using System.Collections.Generic;");
    efCoreEntities.AppendLine("using System.ComponentModel.DataAnnotations;");
    efCoreEntities.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
    efCoreEntities.AppendLine("using Microsoft.EntityFrameworkCore;");
    efCoreEntities.AppendLine("using Microsoft.EntityFrameworkCore.Metadata.Builders;");
    efCoreEntities.AppendLine("using System.Xml.Linq;");
    efCoreEntities.AppendLine();

    // Add the namespace declaration
    efCoreEntities.AppendLine($"namespace {nsWalkerNamespace}.Ef");
    efCoreEntities.AppendLine("{");

    efCoreEntities.AppendLine("    // Entities");
    // Process entities
    foreach (var entity in entities)
    {
        var entityClassName = entity.Name;

        // Generate [Table] attribute
        // Split the table name on "."
        var tableNameParts = entity.TableName.Split('.');
        var tableName = tableNameParts.Length == 2 ? tableNameParts[1] : entity.TableName;
        var schema = tableNameParts.Length == 2 && tableNameParts[0] != "dbo" ? tableNameParts[0] : null;

        // Generate [Table] attribute
        if (schema != null)
        {
            efCoreEntities.AppendLine($"    [Table(\"{tableName}\", Schema = \"{schema}\")]");
        }
        else
        {
            efCoreEntities.AppendLine($"    [Table(\"{tableName}\")]");
        }
        efCoreEntities.AppendLine($"    public class {entityClassName}");
        efCoreEntities.AppendLine("    {");

        // Generate properties
        foreach (var property in entity.Properties)
        {
            if (!string.IsNullOrEmpty(property.ColumnName))
            {
                efCoreEntities.AppendLine($"        [Column(\"{property.ColumnName}\")]");
            }

            // Add MaxLength attribute if applicable (only for string types)
            if (property.MaxLength is not null)
            {
                efCoreEntities.AppendLine($"        [MaxLength({property.MaxLength.Value})]");
            }

            // Generate the property with its type and name
            var propertyType = GetPropertyType(property);
            efCoreEntities.AppendLine($"        public {GetCSharpType(propertyType)} {property.Name} {{ get; set; }}");
        }

        efCoreEntities.AppendLine("    }");
        efCoreEntities.AppendLine();

        // Generate IEntityTypeConfiguration class for the entity
        efCoreEntities.AppendLine($"    internal class {entityClassName}EntityTypeConfiguration : IEntityTypeConfiguration<{entityClassName}>");
        efCoreEntities.AppendLine("    {");
        efCoreEntities.AppendLine($"        public void Configure(EntityTypeBuilder<{entityClassName}> builder)");
        efCoreEntities.AppendLine("        {");

        // Generate primary key configuration
        var primaryKeys = entity.Properties.Where(p => p.IsPrimaryKey).ToList();
        if (primaryKeys.Any())
        {
            var keyNames = string.Join(", ", primaryKeys.Select(pk => $"key.{pk.Name}"));
            efCoreEntities.AppendLine($"            builder.HasKey(key => new {{ {keyNames} }});");
        }

        // Add conversions if needed (for example, XElement conversion)
        foreach (var property in entity.Properties)
        {
            if (property.Type.Contains("XElement"))
            {
                efCoreEntities.AppendLine($"            builder.Property(e => e.{property.Name})");
                efCoreEntities.AppendLine($"                .HasConversion(");
                efCoreEntities.AppendLine($"                    data => data.ToString(),");
                efCoreEntities.AppendLine($"                    data => data != null ? XElement.Parse(data) : null)");
                efCoreEntities.AppendLine($"                .HasColumnType(\"xml\");");
            }
        }

        efCoreEntities.AppendLine("        }");
        efCoreEntities.AppendLine("    }");
        efCoreEntities.AppendLine();
    }

    efCoreEntities.AppendLine("    // Stored Procedure Result Types");
    // Process Stored Procedure results
    foreach (var entity in storedProcedureResults)
    {
        var entityClassName = entity.Name;

        efCoreEntities.AppendLine($"    public class {entityClassName}");
        efCoreEntities.AppendLine("    {");

        // Generate properties
        foreach (var property in entity.Properties)
        {
            //Is this relevant?

            //if (!string.IsNullOrEmpty(property.ColumnName))
            //{
            //    efCoreEntities.AppendLine($"        [Column(\"{property.ColumnName}\")]");
            //}

            //// Add MaxLength attribute if applicable (only for string types)
            //if (property.MaxLength is not null)
            //{
            //    efCoreEntities.AppendLine($"        [MaxLength({property.MaxLength.Value})]");
            //}

            // Generate the property with its type and name
            var propertyType = GetPropertyType(property);
            efCoreEntities.AppendLine($"        public {GetCSharpType(propertyType)} {property.Name} {{ get; set; }}");
        }

        efCoreEntities.AppendLine("    }");
        efCoreEntities.AppendLine();

        // Generate IEntityTypeConfiguration class for the entity
        efCoreEntities.AppendLine($"    internal class {entityClassName}EntityTypeConfiguration : IEntityTypeConfiguration<{entityClassName}>");
        efCoreEntities.AppendLine("    {");
        efCoreEntities.AppendLine($"        public void Configure(EntityTypeBuilder<{entityClassName}> builder)");
        efCoreEntities.AppendLine("        {");

        // Generate primary key configuration
        efCoreEntities.AppendLine($"            builder.HasNoKey();");

        //TODO: do we need mapping in sproc results?
        //// Add conversions if needed (for example, XElement conversion)
        //foreach (var property in entity.Properties)
        //{
        //    if (property.Type.Contains("XElement"))
        //    {
        //        efCoreEntities.AppendLine($"            builder.Property(e => e.{property.Name})");
        //        efCoreEntities.AppendLine($"                .HasConversion(");
        //        efCoreEntities.AppendLine($"                    data => data.ToString(),");
        //        efCoreEntities.AppendLine($"                    data => data != null ? XElement.Parse(data) : null)");
        //        efCoreEntities.AppendLine($"                .HasColumnType(\"xml\");");
        //    }
        //}

        efCoreEntities.AppendLine("        }");
        efCoreEntities.AppendLine("    }");
        efCoreEntities.AppendLine();
    }

    // Close the namespace
    efCoreEntities.AppendLine("}");

    return efCoreEntities.ToString();
}

static string GetPropertyType(EntityProperty property)
{
    var propertyType = property.Type;

    if (propertyType.StartsWith("EntitySet<"))
    {
        propertyType = propertyType.Replace("EntitySet<", "List<");
    }
    else if (propertyType == "System.Data.Linq.Binary")
    {
        propertyType = "byte[]";
    }
    else if (propertyType == "System.Xml.Linq.XElement")
    {
        propertyType = "XElement";
    }
    else if (propertyType.StartsWith("System.Nullable<"))
    {
        propertyType = Regex.Replace(propertyType, @"System\.Nullable<([^>]+)>", "$1?");
    }

    return propertyType;
}


static string GenerateDbContext(DataContext dataContext, string nsWalkerNamespace,
    List<StoredProcedureResult> storedProcedureResults)
{
    var dbContext = new StringBuilder();

    // Add necessary usings
    dbContext.AppendLine("using System;");
    dbContext.AppendLine("using System.Collections.Generic;");
    dbContext.AppendLine("using CAB.CSP.Common.Server.DataAccess;");
    dbContext.AppendLine("using CAB.CSP.DataAccess;");
    dbContext.AppendLine("using Microsoft.EntityFrameworkCore;");
    dbContext.AppendLine("using System.Linq;");
    dbContext.AppendLine("using System.Data;");
    dbContext.AppendLine("using Microsoft.Data.SqlClient;");
    dbContext.AppendLine("using DbContext = CAB.CSP.Common.Server.DataAccess.DbContext;");
    dbContext.AppendLine();

    // Add the namespace declaration
    dbContext.AppendLine($"namespace {nsWalkerNamespace}.Ef");
    dbContext.AppendLine("{");

    // Class declaration
    dbContext.AppendLine($"    public class {dataContext.Name}DbContext : DbContext");
    dbContext.AppendLine("    {");

    // Constructor with dependencies
    dbContext.AppendLine($"        public {dataContext.Name}DbContext(IConnectionStringProvider connectionStringProvider, ITimeoutConfigurationProvider timeoutConfigurationProvider, AzureAdAuthenticationDbConnectionInterceptor adAuthenticationDbConnectionInterceptor) : base(connectionStringProvider, timeoutConfigurationProvider, adAuthenticationDbConnectionInterceptor)");
    dbContext.AppendLine("        {");
    dbContext.AppendLine("        }");
    dbContext.AppendLine();

    // DbSet properties for tables
    foreach (var table in dataContext.Tables)
    {
        dbContext.AppendLine($"        public DbSet<{table.EntityType}> {table.Name} {{ get; set; }}");
    }

    dbContext.AppendLine();

    // OnModelCreating override with ApplyConfiguration for each entity
    dbContext.AppendLine("        protected override void OnModelCreating(ModelBuilder modelBuilder)");
    dbContext.AppendLine("        {");
    dbContext.AppendLine("            base.OnModelCreating(modelBuilder);");
    dbContext.AppendLine();

    // Apply configuration for each entity's EntityTypeConfiguration
    foreach (var table in dataContext.Tables)
    {
        dbContext.AppendLine($"            modelBuilder.ApplyConfiguration(new {table.EntityType}EntityTypeConfiguration());");
    }

    foreach (var table in storedProcedureResults)
    {
        dbContext.AppendLine($"            modelBuilder.ApplyConfiguration(new {table.Name}EntityTypeConfiguration());");
    }


    dbContext.AppendLine("        }");
    dbContext.AppendLine();

    // Stored procedure methods
    foreach (var sp in dataContext.StoredProcedures)
    {
        // Define the method signature
        var returnType = ReplaceSingleResult(sp);
        if (returnType == "int")
        {
            dbContext.AppendLine($"        public int {sp.MethodName}(");
        }
        else
        {
            dbContext.AppendLine($"        public IEnumerable<{GetCSharpType(returnType)}> {sp.MethodName}(");
        }

        // Add parameters to method signature
        var parameters = sp.Parameters.Select(p => $"{GetCSharpType(p.Type)} {MethodParamName(p.Name)}");
        dbContext.AppendLine($"            {string.Join(", ", parameters)}");
        dbContext.AppendLine("        )");
        dbContext.AppendLine("        {");

        // Begin 'using' block
        dbContext.AppendLine("            using (SqlParameterExtensions.ParameterTypeFromContext(this))");
        dbContext.AppendLine("            {");

        // Define each parameter separately in the 'using' block
        foreach (var param in sp.Parameters)
        {
            //"GuidTVP"
            // var organizationIdsParam = "@organizationIds".CreateTvp(GuidTvpName, organizationIds);
            if (param.Type == "object")
            {
                dbContext.AppendLine($"                var {MethodParamName(param.Name)}Param = \"@{param.Name}\".CreateTvp(\"GuidTVP\", {MethodParamName(param.Name)});");
            }
            else
            {
                dbContext.AppendLine($"                var {MethodParamName(param.Name)}Param = \"@{param.Name}\".Create({MethodParamName(param.Name)});");
            }
        }

        // Execute the stored procedure with parameter variables
        var sqlParamNames = string.Join(", ", sp.Parameters.Select(p => $"@{p.Name}"));
        var sqlParams = string.Join(", ", sp.Parameters.Select(p => $"{MethodParamName(p.Name)}Param"));
        if (sqlParams.Length > 0)
        {
            sqlParams = ", " + sqlParams;
        }

        if (returnType == "int")
        {
            dbContext.AppendLine($"                var res = Database.ExecuteSqlRaw(@\"EXEC {sp.MethodName} {sqlParamNames}\" {sqlParams});");
        }
        else
        {
            if (sp.MethodName.StartsWith("Fill") && sp.StoredProcName.ToLowerInvariant().Trim().StartsWith("select"))
            {
                //Hack for datasets
                dbContext.AppendLine($"                var res = this.Set<{returnType}>().FromSqlRaw(@\"{sp.StoredProcName} {sqlParamNames}\" {sqlParams}).AsEnumerable();");
            }
            else
            {
                dbContext.AppendLine($"                var res = this.Set<{returnType}>().FromSqlRaw(@\"EXEC {sp.StoredProcName} {sqlParamNames}\" {sqlParams}).AsEnumerable();");
            }
        }

        dbContext.AppendLine($"               return res;"); 

        // Close 'using' block
        dbContext.AppendLine("            }");

        dbContext.AppendLine("        }");
        dbContext.AppendLine();
    }

    dbContext.AppendLine("    }");

    // Close the namespace
    dbContext.AppendLine("}");

    return dbContext.ToString();
}


static string ReplaceSingleResult(StoredProcedureMapping? sp)
{
    return sp.ReturnType.Replace("ISingleResult<", "").Replace(">", "");
}

static string GetSqlDbType(string csharpType)
{
    return csharpType switch
    {
        "int" => "Int",
        "string" => "NVarChar",
        "Guid" => "UniqueIdentifier",
        "DateTime" => "DateTime",
        "bool" => "Bit",
        _ => "NVarChar", // Default to NVarChar for unknown types
    };
}


static string GetCSharpType(string csharpType)
{
    return csharpType switch
    {
        // Integer types
        "System.Byte" => "byte",
        "System.Nullable<System.Byte>" => "byte?",
        "System.Byte?" => "byte?",

        "System.SByte" => "sbyte",
        "System.Nullable<System.SByte>" => "sbyte?",
        "System.SByte?" => "sbyte?",

        "System.Int16" => "short",
        "System.Nullable<System.Int16>" => "short?",
        "System.Int16?" => "short?",

        "System.UInt16" => "ushort",
        "System.Nullable<System.UInt16>" => "ushort?",
        "System.UInt16?" => "ushort?",



        "System.Int32" => "int",
        "System.Nullable<int>" => "int?",
        "System.Nullable<System.Int32>" => "int?",
        "System.Int32?" => "int?",

        "System.UInt32" => "uint",
        "System.Nullable<System.UInt32>" => "uint?",
        "System.UInt32?" => "uint?",

        "System.Int64" => "long",
        "System.Nullable<System.Int64>" => "long?",
        "System.Int64?" => "long?",

        "System.UInt64" => "ulong",
        "System.Nullable<System.UInt64>" => "ulong?",
        "System.UInt64?" => "ulong?",

        // Floating-point types
        "System.Single" => "float",
        "System.Nullable<System.Single>" => "float?",
        "System.Single?" => "float?",

        "System.Double" => "double",
        "System.Nullable<System.Double>" => "double?",
        "System.Double?" => "double?",

        "System.Decimal" => "decimal",
        "System.Nullable<System.Decimal>" => "decimal?",
        "System.Decimal?" => "decimal?",

        // Boolean type
        "System.Boolean" => "bool",
        "System.Nullable<System.Boolean>" => "bool?",
        "System.Boolean?" => "bool?",

        // Character type
        "System.Char" => "char",
        "System.Nullable<System.Char>" => "char?",
        "System.Char?" => "char?",

        // String type
        "System.String" => "string",

        // Date and time types
        "System.DateTime" => "DateTime",
        "System.Nullable<System.DateTime>" => "DateTime?",
        "System.DateTime?" => "DateTime?",
        "System.DateTimeOffset" => "DateTimeOffset",
        "System.Nullable<System.DateTimeOffset>" => "DateTimeOffset?",

        // Guid type
        "System.Guid" => "Guid",
        "System.Nullable<System.Guid>" => "Guid?",
        "System.Guid?" => "Guid?",

        // Object type
        "System.Object" => "object",
        "object" => "IEnumerable<Guid>",
        "global::System.Nullable<global::System.Guid>" => "Guid?",
        "global::System.Nullable<global::System.DateTimeOffset>" => "DateTimeOffset?",
        "global::System.Nullable<int>" => "int?",
        // Default case
        _ => csharpType,
    };
}
static string MethodParamName(string input)
{
    if (string.IsNullOrEmpty(input) || char.IsLower(input[0]))
        return input;

    return char.ToLower(input[0]) + input.Substring(1);
}

