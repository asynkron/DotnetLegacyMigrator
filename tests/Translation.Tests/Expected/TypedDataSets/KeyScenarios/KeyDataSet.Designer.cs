using System.Data;

public class CompositeTableDataTable : DataTable
{
    public CompositeTableDataTable()
    {
        this.TableName = "CompositeTable";
    }
}

public class IdentityTableDataTable : DataTable
{
    public IdentityTableDataTable()
    {
        this.TableName = "IdentityTable";
    }
}
