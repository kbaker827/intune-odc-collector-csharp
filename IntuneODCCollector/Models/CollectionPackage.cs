using System.Collections.Generic;
using System.Xml.Linq;

namespace IntuneODCCollector.Models;

public record CollectionPackage(
    string Id,
    IReadOnlyList<XElement> Files,
    IReadOnlyList<XElement> Registries,
    IReadOnlyList<XElement> EventLogs,
    IReadOnlyList<XElement> Commands
);
