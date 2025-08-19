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
        var sb = new StringBuilder();
        sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
        sb.AppendLine();
        foreach (var entity in entities)
        {
            sb.AppendLine($"[Table(\"{entity.TableName}\")]");
            sb.AppendLine($"public class {entity.Name}");
            sb.AppendLine("{");
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
            sb.AppendLine("}");
            sb.AppendLine();
        }
        return sb.ToString().Trim();
    }

    public static string GenerateDataContext(DataContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();
        sb.AppendLine($"public class {context.Name} : DbContext");
        sb.AppendLine("{");
        foreach (var table in context.Tables)
            sb.AppendLine($"    public DbSet<{table.EntityType}> {table.Name} {{ get; set; }}");
        sb.AppendLine("}");
        return sb.ToString().Trim();
    }
}
