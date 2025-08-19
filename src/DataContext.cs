namespace LinqToSqlMetadataExtractor;

class DataContext
{
    public string Name { get; set; }
    public List<TableMapping> Tables { get; set; }
    public List<StoredProcedureMapping> StoredProcedures { get; set; } = new List<StoredProcedureMapping>();
}