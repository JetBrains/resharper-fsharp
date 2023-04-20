using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DocComments
{
  public class XmlDocBlockNodeType : FSharpCompositeNodeType
  {
    public static readonly CompositeNodeType Instance = new XmlDocBlockNodeType();

    private XmlDocBlockNodeType() : base("XML_DOC_BLOCK", 1900)
    {
    }

    public override CompositeElement Create() => new XmlDocBlock();
  }
}
