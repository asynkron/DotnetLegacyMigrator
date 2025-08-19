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

    private static string NormalizeType(string type) =>
        _primitiveAliases.TryGetValue(type.TrimEnd('?'), out var alias)
            ? alias + (type.EndsWith("?") ? "?" : string.Empty)
            : type;

    public static string GenerateEntities(IEnumerable<Entity> entities)
    {
        ResolveInverses(entities);
        var sb = new StringBuilder();
        sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        // Sort entities to produce deterministic output
        foreach (var entity in entities.OrderBy(e => e.Name))
        {
            sb.AppendLine($"[Table(\"{entity.TableName}\")]");
            sb.AppendLine($"public class {entity.Name}");
            sb.AppendLine("{");
            // Preserve declaration order; input walkers already visit primary key first
            foreach (var prop in entity.Properties)
            {
                if (prop.IsPrimaryKey)
                    sb.AppendLine("    [Key]");
                if (prop.IsDbGenerated)
                    sb.AppendLine("    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
                var columnName = string.IsNullOrWhiteSpace(prop.ColumnName) ? prop.Name : prop.ColumnName;
                var columnArgs = new List<string> { $"\"{columnName}\"" };
                if (!string.IsNullOrWhiteSpace(prop.DbType))
                    columnArgs.Add($"TypeName = \"{prop.DbType}\"");
                sb.AppendLine($"    [Column({string.Join(", ", columnArgs)})]");
                sb.AppendLine($"    public {NormalizeType(prop.Type)} {prop.Name} {{ get; set; }}");
                sb.AppendLine();
            }

            foreach (var nav in entity.Navigations)
            {
                if (!string.IsNullOrWhiteSpace(nav.Inverse))
                    sb.AppendLine($"    [InverseProperty(nameof({nav.TargetEntity}.{nav.Inverse}))]");
                if (!string.IsNullOrWhiteSpace(nav.ForeignKey))
                    sb.AppendLine($"    [ForeignKey(\"{nav.ForeignKey}\")]");
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
        sb.AppendLine("}");
        return sb.ToString().Trim();
    }
}
