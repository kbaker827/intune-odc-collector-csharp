using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using IntuneODCCollector.Models;

namespace IntuneODCCollector.Services;

public class XmlParserService
{
    public List<CollectionPackage> Parse(string xmlPath)
    {
        var doc = XDocument.Load(xmlPath);
        var root = doc.Root!;
        var ns = root.Name.Namespace;

        var packageElements = root.Descendants(ns + "Package").ToList();
        if (!packageElements.Any())
            packageElements = root.Descendants()
                .Where(e => e.Name.LocalName == "Package")
                .ToList();

        var result = new List<CollectionPackage>();
        foreach (var pkg in packageElements)
        {
            var id = pkg.Attribute("ID")?.Value ?? "Unknown";
            result.Add(new CollectionPackage(
                Id: id,
                Files: Children(pkg, ns, "Files", "File"),
                Registries: Children(pkg, ns, "Registries", "Registry"),
                EventLogs: Children(pkg, ns, "EventLogs", "EventLog"),
                Commands: Children(pkg, ns, "Commands", "Command")
            ));
        }
        return result;
    }

    private static IReadOnlyList<XElement> Children(
        XElement pkg, XNamespace ns, string parentTag, string childTag)
    {
        var parent = pkg.Element(ns + parentTag)
                     ?? pkg.Elements().FirstOrDefault(e => e.Name.LocalName == parentTag);
        if (parent is null) return [];

        var children = parent.Elements(ns + childTag).ToList();
        if (children.Any()) return children;

        return parent.Elements()
            .Where(e => e.Name.LocalName == childTag)
            .ToList();
    }
}
