using System.Xml;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;
using static FSharp.Compiler.TypeProviders;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class ProvidedMemberUtils
  {
    public static XmlNode GetXmlDoc(this IProvidedCustomAttributeProvider provider, ITypeMember element, bool expand)
    {
      var xmlDocs = provider.GetXmlDocAttributes(null);
      if (XMLDocUtil.Load(xmlDocs, element, out var node) && expand)
      {
        XMLDocUtil.ExtendWithInheritedDocTag(element, node);
      }

      return node;
    }
  }
}
