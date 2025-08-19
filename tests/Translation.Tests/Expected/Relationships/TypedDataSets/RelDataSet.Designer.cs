using System.Data;

public class CustomerDataTable : DataTable
{
    public CustomerDataTable()
    {
        this.TableName = "Customer";
    }
}

public class OrderDataTable : DataTable
{
    public OrderDataTable()
    {
        this.TableName = "Order";
    }
}
