using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DocComments;

public class FSharpWarningDirectiveNodeType : FSharpCompositeNodeType
{
  public static readonly CompositeNodeType Instance = new FSharpWarningDirectiveNodeType();
    
  private FSharpWarningDirectiveNodeType() : base("WARNING_DIRECTIVE", 1901)
  {
  }

  public override CompositeElement Create() => new FSharpWarningDirective();
}
