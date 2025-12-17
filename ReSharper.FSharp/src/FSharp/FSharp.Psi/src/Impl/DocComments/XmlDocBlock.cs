using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DocComments
{
  public class DocCommentBlockNodeType : FSharpCompositeNodeType
  {
    public static readonly CompositeNodeType Instance = new DocCommentBlockNodeType();

    private DocCommentBlockNodeType() : base("XML_DOC_BLOCK", 1900, NodeTypeFlags.None)
    {
    }

    public override CompositeElement Create() => new XmlDocBlock();
  }
}
