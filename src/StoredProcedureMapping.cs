namespace LinqToSqlMetadataExtractor;

class StoredProcedureMapping
{
    public string MethodName { get; set; }
    public string ReturnType { get; set; }
    public List<ParameterMapping> Parameters { get; set; } = new List<ParameterMapping>();
    public string StoredProcName { get; set; }
}