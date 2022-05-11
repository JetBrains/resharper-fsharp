using System.Xml;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Xml.XmlDocComments;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class ProvidedMemberUtils
  {
    public static XmlNode GetXmlDoc(this IProvidedCustomAttributeProvider provider, ITypeMember element)
    {
      var xmlDocs = provider.GetXmlDocAttributes(null);
      DocCommentBlockUtil.TryGetXml(xmlDocs, element, out var node);
      return node;
    }
  }
}
