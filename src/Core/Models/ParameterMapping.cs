namespace DotnetLegacyMigrator.Models;

/// <summary>
/// Describes a stored procedure parameter.
/// </summary>
public class ParameterMapping
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
}
