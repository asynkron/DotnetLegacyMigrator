namespace DotnetLegacyMigrator.Models;

/// <summary>
/// Describes a stored procedure parameter.
/// </summary>
public class ParameterMapping
{
    public string Name { get; set; }
    public string Type { get; set; }
    public bool IsNullable { get; set; }
}
