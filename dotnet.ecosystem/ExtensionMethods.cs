using System.Xml.Linq;

namespace dotnet.ecosystem;

public static class ExtensionMethods
{
    public static XAttribute? AttributeCaseInsensitive(this XElement element, string nameToLookFor)
    {
        return element.Attributes().FirstOrDefault(x => string.Equals(x.Name.LocalName, nameToLookFor, StringComparison.InvariantCultureIgnoreCase));
    }
}