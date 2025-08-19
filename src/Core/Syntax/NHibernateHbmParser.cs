using System.Xml.Linq;
using DotnetLegacyMigrator.Models;

namespace DotnetLegacyMigrator.Syntax;

/// <summary>
/// Minimal parser for NHibernate .hbm.xml mapping files to reuse existing
/// metadata models.
/// </summary>
public static class NHibernateHbmParser
{
    private static readonly XNamespace Ns = "urn:nhibernate-mapping-2.2";

    public static (DataContext Context, List<Entity> Entities) ParseFiles(IEnumerable<string> hbmFiles)
    {
        var entities = new List<Entity>();
        var tables = new List<TableMapping>();
        string? assembly = null;

        foreach (var file in hbmFiles)
        {
            var doc = XDocument.Load(file);
            var root = doc.Element(Ns + "hibernate-mapping");
            assembly ??= root?.Attribute("assembly")?.Value;
            foreach (var classEl in root?.Elements(Ns + "class") ?? Enumerable.Empty<XElement>())
            {
                var name = classEl.Attribute("name")?.Value ?? "Entity";
                var table = classEl.Attribute("table")?.Value ?? name;
                var props = new List<EntityProperty>();

                var idEl = classEl.Element(Ns + "id");
                if (idEl != null)
                {
                    var idName = idEl.Attribute("name")?.Value ?? "Id";
                    var type = idEl.Attribute("type")?.Value ?? "Int32";
                    var gen = idEl.Element(Ns + "generator");
                    props.Add(new EntityProperty
                    {
                        Name = idName,
                        Type = type,
                        ColumnName = idName,
                        IsPrimaryKey = true,
                        IsDbGenerated = gen != null
                    });
                }

                foreach (var p in classEl.Elements(Ns + "property"))
                {
                    var propName = p.Attribute("name")?.Value ?? "Prop";
                    var type = p.Attribute("type")?.Value ?? "String";
                    props.Add(new EntityProperty
                    {
                        Name = propName,
                        Type = type,
                        ColumnName = propName
                    });
                }

                entities.Add(new Entity
                {
                    Name = name,
                    TableName = table,
                    Properties = props
                });

                tables.Add(new TableMapping { Name = table, EntityType = name });
            }
        }

        var contextName = (assembly ?? "NHibernate") + "Context";
        var ctx = new DataContext
        {
            Name = contextName,
            Tables = tables,
            StoredProcedures = new List<StoredProcedureMapping>()
        };

        return (ctx, entities);
    }
}
