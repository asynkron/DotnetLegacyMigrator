using System.Text;
using DotnetLegacyMigrator.Models;

namespace DotnetLegacyMigrator;

public static class CodeGenerator
{
    private static readonly Dictionary<string, string> _primitiveAliases = new()
    {
        ["String"] = "string",
        ["Int32"] = "int",
        ["Int16"] = "short",
        ["Int64"] = "long",
        ["Boolean"] = "bool",
        ["Decimal"] = "decimal",
        ["Double"] = "double",
        ["Single"] = "float",
        ["Byte"] = "byte"
    };

    private static readonly string[] _baseUsings =
    {
        "System.Collections.Generic"
    };

    private static string NormalizeType(string type, ISet<string> usings)
    {
        // handle nullable suffix
        var isNullable = type.EndsWith("?");
        if (isNullable)
            type = type[..^1];

        // handle System.Nullable<T> or Nullable<T>
        if (type.StartsWith("System.Nullable<") || type.StartsWith("Nullable<"))
        {
            var inner = type.Substring(type.IndexOf('<') + 1);
            inner = inner.TrimEnd('>');
            return NormalizeType(inner, usings) + "?";
        }

        string? ns = null;
        var lastDot = type.LastIndexOf('.');
        if (lastDot > 0)
        {
            ns = type[..lastDot];
            type = type[(lastDot + 1)..];
            if (!string.Equals(ns, "System", StringComparison.Ordinal))
                usings.Add(ns);
        }

        if (_primitiveAliases.TryGetValue(type, out var alias))
            type = alias;

        return type + (isNullable ? "?" : string.Empty);
    }

    public static string GenerateEntities(IEnumerable<Entity> entities)
    {
        ResolveInverses(entities);
        var extraUsings = new HashSet<string>();

        // pre-scan property types to collect required namespaces
        foreach (var prop in entities.SelectMany(e => e.Properties))
            _ = NormalizeType(prop.Type, extraUsings);

        var sb = new StringBuilder();
        foreach (var u in _baseUsings)
            sb.AppendLine($"using {u};");
        foreach (var u in extraUsings.OrderBy(u => u))
            sb.AppendLine($"using {u};");
        sb.AppendLine();

        // Sort entities to produce deterministic output
        foreach (var entity in entities.OrderBy(e => e.Name))
        {
            sb.AppendLine($"public class {entity.Name}");
            sb.AppendLine("{");
            // Preserve declaration order; input walkers already visit primary key first
            foreach (var prop in entity.Properties)
            {
                sb.AppendLine($"    public {NormalizeType(prop.Type, extraUsings)} {prop.Name} {{ get; set; }}");
                sb.AppendLine();
            }

            foreach (var nav in entity.Navigations)
            {
                var type = nav.IsCollection ? $"List<{nav.TargetEntity}>" : nav.TargetEntity;
                var init = nav.IsCollection ? " = new();" : string.Empty;
                sb.AppendLine($"    public {type} {nav.Name} {{ get; set; }}{init}");
                sb.AppendLine();
            }
            sb.AppendLine("}");
            sb.AppendLine();
        }
        return sb.ToString().Trim();
    }

    public static string GenerateEntityConfigurations(IEnumerable<Entity> entities)
    {
        var extraUsings = new HashSet<string>();
        foreach (var prop in entities.SelectMany(e => e.Properties))
        {
            if (IsXmlType(prop.Type))
                extraUsings.Add("System.Xml.Linq");
        }

        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine("using Microsoft.EntityFrameworkCore.Metadata.Builders;");
        foreach (var u in extraUsings.OrderBy(u => u))
            sb.AppendLine($"using {u};");
        sb.AppendLine();

        foreach (var entity in entities.OrderBy(e => e.Name))
        {
            sb.AppendLine($"public class {entity.Name}Configuration : IEntityTypeConfiguration<{entity.Name}>");
            sb.AppendLine("{");
            sb.AppendLine($"    public void Configure(EntityTypeBuilder<{entity.Name}> builder)");
            sb.AppendLine("    {");
            sb.AppendLine($"        builder.ToTable(\"{entity.TableName}\");");

            var keys = entity.Properties.Where(p => p.IsPrimaryKey).ToList();
            if (keys.Count == 1)
                sb.AppendLine($"        builder.HasKey(e => e.{keys[0].Name});");
            else if (keys.Count > 1)
                sb.AppendLine($"        builder.HasKey(e => new {{ {string.Join(", ", keys.Select(k => $"e.{k.Name}"))} }});");

            foreach (var prop in entity.Properties)
            {
                var columnName = string.IsNullOrWhiteSpace(prop.ColumnName) ? prop.Name : prop.ColumnName;
                var calls = new List<string> {$".HasColumnName(\"{columnName}\")"};
                if (!string.IsNullOrWhiteSpace(prop.DbType))
                    calls.Add($".HasColumnType(\"{prop.DbType}\")");
                if (prop.IsDbGenerated)
                    calls.Add(".ValueGeneratedOnAdd()");
                if (IsXmlType(prop.Type))
                    calls.Add(".HasConversion(v => v.ToString(), v => XElement.Parse(v))");
                sb.AppendLine($"        builder.Property(e => e.{prop.Name})");
                sb.AppendLine($"            {string.Join("\n            ", calls)};");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
        }
        return sb.ToString().Trim();
    }

    private static bool IsXmlType(string type)
    {
        var t = type.TrimEnd('?');
        return t == "System.Xml.Linq.XElement" || t == "XElement";
    }

    private static void ResolveInverses(IEnumerable<Entity> entities)
    {
        var allNavs = entities.SelectMany(e => e.Navigations.Select(n => (Entity: e, Nav: n))).ToList();

        foreach (var group in allNavs.Where(t => !string.IsNullOrWhiteSpace(t.Nav.AssociationName))
                                      .GroupBy(t => t.Nav.AssociationName))
        {
            var pair = group.ToList();
            if (pair.Count == 2)
            {
                pair[0].Nav.Inverse = pair[1].Nav.Name;
                pair[1].Nav.Inverse = pair[0].Nav.Name;
            }
        }

        foreach (var group in allNavs.Where(t => !string.IsNullOrWhiteSpace(t.Nav.JoinTable))
                                      .GroupBy(t => t.Nav.JoinTable))
        {
            var pair = group.ToList();
            if (pair.Count == 2)
            {
                pair[0].Nav.Inverse = pair[1].Nav.Name;
                pair[1].Nav.Inverse = pair[0].Nav.Name;
            }
        }
    }
    public static string GenerateDataContext(DataContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();
        sb.AppendLine($"public class {context.Name} : DbContext");
        sb.AppendLine("{");
        // Order tables by name so DbSet properties are emitted in a stable order
        foreach (var table in context.Tables.OrderBy(t => t.Name))
            sb.AppendLine($"    public DbSet<{table.EntityType}> {table.Name} {{ get; set; }}");
        sb.AppendLine();
        sb.AppendLine("    protected override void OnModelCreating(ModelBuilder modelBuilder)");
        sb.AppendLine("    {");
        foreach (var table in context.Tables.OrderBy(t => t.EntityType))
            sb.AppendLine($"        modelBuilder.ApplyConfiguration(new {table.EntityType}Configuration());");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString().Trim();
    }
}
