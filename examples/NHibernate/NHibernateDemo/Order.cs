namespace NHibernateDemo;

// Second entity to expand coverage
public class Order
{
    public virtual int Id { get; set; }

    // Non-null primitive property
    public virtual int CustomerId { get; set; }

    // Non-null string with length specified in mapping
    public virtual string Description { get; set; } = string.Empty;

    // Nullable string with length specified in mapping
    public virtual string? Notes { get; set; }
}
