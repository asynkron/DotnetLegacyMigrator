namespace NHibernateDemo;

// Simple POCO entity mapped in Customer.hbm.xml
public class Customer
{
    public virtual int Id { get; set; }
    public virtual string? Name { get; set; }
}
