namespace NHibernateDemo;

// Simple POCO entity mapped in Customer.hbm.xml
public class Customer
{
    public virtual int Id { get; set; }

    // Non-null string with length defined in mapping
    public virtual string Name { get; set; } = string.Empty;

    // Nullable primitive property
    public virtual int? Age { get; set; }
}
