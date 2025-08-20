namespace DotnetLegacyMigrator.Models;

/// <summary>
/// Describes a stored procedure parameter.
/// </summary>
public class ParameterMapping
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsNullable { get; set; }

    /// <summary>
    /// Parameter direction such as Input, Output, or InputOutput.
    /// </summary>
    public string Direction { get; set; } = "Input";

    /// <summary>
    /// Size of variable-length parameters when specified.
    /// </summary>
    public int? Size { get; set; }
}
