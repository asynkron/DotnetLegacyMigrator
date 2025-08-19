using System;

namespace System.Data.Linq.Mapping;

[AttributeUsage(AttributeTargets.Class)]
public sealed class TableAttribute : Attribute
{
    public string? Name { get; set; }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class DatabaseAttribute : Attribute
{
    public string? Name { get; set; }
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class ColumnAttribute : Attribute
{
    public bool IsPrimaryKey { get; set; }
    public bool CanBeNull { get; set; }
    public bool IsDbGenerated { get; set; }
    public string? DbType { get; set; }
    public string? Name { get; set; }
}
