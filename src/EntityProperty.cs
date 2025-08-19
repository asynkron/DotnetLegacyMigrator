namespace LinqToSqlMetadataExtractor;

class EntityProperty
{
    public string Name { get; set; }
    public string Type { get; set; }
    public string Metadata { get; set; }
    public string ColumnName { get; set; }
    public string DbType { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsDbGenerated { get; set; }
    public bool IsNullable { get; set; }
    public int Order { get; set; }

    public int? MaxLength { get; set; }  // Add this to capture the MaxLength


    //l2s relations
    public string ForeignKey { get; set; }

    public string ForeignEntity { get; set; }

    public bool ForeignMany { get; set; }
    public bool SelfMany { get; set; }

    public bool CascadeDelete { get; set; }


    /*
     l2s attribs
     [global::System.Data.Linq.Mapping.AssociationAttribute(Name="DbEstimate_DbEstimateWorkshop", Storage="_DbEstimate", ThisKey="EstimateId", OtherKey="Id", IsForeignKey=true, DeleteOnNull=true, DeleteRule="CASCADE")]

    should map like so:
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbEstimateWorkshop>()
        .HasOne(e => e.DbEstimate) // Navigation property
        .WithMany() // Or specify navigation collection if applicable
        .HasForeignKey(e => e.EstimateId) // Foreign key property
        .OnDelete(DeleteBehavior.Cascade); // Cascade delete behavior
    }

    */
}
