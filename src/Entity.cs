namespace LinqToSqlMetadataExtractor;

class Entity
{
    public string Name { get; set; }
    public List<EntityProperty> Properties { get; set; }
    public string TableName { get; set; }
}


class StoredProcedureResult
{
    public string Name { get; set; }
    public List<EntityProperty> Properties { get; set; }
}
