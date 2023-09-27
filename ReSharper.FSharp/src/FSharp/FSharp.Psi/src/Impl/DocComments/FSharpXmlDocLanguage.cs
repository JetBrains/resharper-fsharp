using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Xml.XmlDocComments;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DocComments
{
  [LanguageDefinition(Name)]
  public class FSharpXmlDocLanguage : XmlDocLanguage
  {
    private new const string Name = "FSHARP_XMLDOC";

    [CanBeNull, UsedImplicitly] public new static FSharpXmlDocLanguage Instance { get; private set; }

    protected FSharpXmlDocLanguage() : base(Name, "F# XmlDoc")
    {
    }
  }
}
