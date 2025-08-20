using System;
using System.Linq;
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
        var sprocs = new List<StoredProcedureMapping>();
        string? assembly = null;

        foreach (var file in hbmFiles)
        {
            var doc = XDocument.Load(file);
            var root = doc.Element(Ns + "hibernate-mapping");
            assembly ??= root?.Attribute("assembly")?.Value;

            // Parse mapped entities
            foreach (var classEl in root?.Elements(Ns + "class") ?? Enumerable.Empty<XElement>())
            {
                var name = classEl.Attribute("name")?.Value ?? "Entity";
                var table = classEl.Attribute("table")?.Value ?? name;
                var schema = classEl.Attribute("schema")?.Value;
                var props = new List<EntityProperty>();
                var navs = new List<Navigation>();

                var idEl = classEl.Element(Ns + "id");
                if (idEl != null)
                {
                    var idName = idEl.Attribute("name")?.Value ?? "Id";
                    var type = idEl.Attribute("type")?.Value ?? "Int32";
                    var length = idEl.Attribute("length")?.Value;
                    var gen = idEl.Element(Ns + "generator");
                    var dbType = length != null && type.Equals("String", StringComparison.OrdinalIgnoreCase)
                        ? $"NVARCHAR({length})"
                        : null;
                    props.Add(new EntityProperty
                    {
                        Name = idName,
                        Type = type,
                        ColumnName = idName,
                        IsPrimaryKey = true,
                        IsDbGenerated = gen != null,
                        DbType = dbType
                    });
                }

                foreach (var p in classEl.Elements(Ns + "property"))
                {
                    var propName = p.Attribute("name")?.Value ?? "Prop";
                    var type = p.Attribute("type")?.Value ?? "String";
                    var notNull = p.Attribute("not-null")?.Value == "true";
                    var length = p.Attribute("length")?.Value;
                    var normalizedType = !notNull && !type.Equals("String", StringComparison.OrdinalIgnoreCase)
                        ? type + "?"
                        : type;
                    if (!notNull && type.Equals("String", StringComparison.OrdinalIgnoreCase))
                        normalizedType = "String?";
                    var dbType = length != null && type.Equals("String", StringComparison.OrdinalIgnoreCase)
                        ? $"NVARCHAR({length})"
                        : null;
                    props.Add(new EntityProperty
                    {
                        Name = propName,
                        Type = normalizedType,
                        ColumnName = propName,
                        DbType = dbType
                    });
                }

                foreach (var m2o in classEl.Elements(Ns + "many-to-one"))
                {
                    var navName = m2o.Attribute("name")?.Value ?? "Nav";
                    var target = m2o.Attribute("class")?.Value ?? "";
                    var column = m2o.Attribute("column")?.Value;
                    navs.Add(new Navigation
                    {
                        Name = navName,
                        TargetEntity = target,
                        ForeignKey = column
                    });
                }

                foreach (var set in classEl.Elements(Ns + "set").Concat(classEl.Elements(Ns + "bag")))
                {
                    var navName = set.Attribute("name")?.Value ?? "Nav";
                    var tableAttr = set.Attribute("table")?.Value;
                    var oneToMany = set.Element(Ns + "one-to-many");
                    var manyToMany = set.Element(Ns + "many-to-many");
                    var target = oneToMany?.Attribute("class")?.Value ?? manyToMany?.Attribute("class")?.Value ?? "";
                    var keyColumn = set.Element(Ns + "key")?.Attribute("column")?.Value;
                    navs.Add(new Navigation
                    {
                        Name = navName,
                        TargetEntity = target,
                        IsCollection = true,
                        ForeignKey = keyColumn,
                        JoinTable = tableAttr
                    });
                }

                entities.Add(new Entity
                {
                    Name = name,
                    TableName = table,
                    Schema = schema,
                    Properties = props,
                    Navigations = navs
                });

                tables.Add(new TableMapping { Name = table, EntityType = name, Schema = schema, Navigations = navs });
            }

            // Parse simple sql-query elements representing stored procedures
            foreach (var sql in root?.Elements(Ns + "sql-query") ?? Enumerable.Empty<XElement>())
            {
                var name = sql.Attribute("name")?.Value ?? "Proc";
                var returnType = sql.Element(Ns + "return")?.Attribute("class")?.Value ?? "int";
                sprocs.Add(new StoredProcedureMapping
                {
                    MethodName = name,
                    StoredProcName = name,
                    ReturnType = returnType,
                    Parameters = new List<ParameterMapping>()
                });
            }
        }

        var contextName = (assembly ?? "NHibernate") + "Context";
        var ctx = new DataContext
        {
            Name = contextName,
            Tables = tables,
            StoredProcedures = sprocs
        };

        return (ctx, entities);
    }
}
