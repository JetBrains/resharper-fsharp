using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class OpenStatementReference : FSharpSymbolReference
  {
    public OpenStatementReference([NotNull] IFSharpReferenceOwner owner) : base(owner)
    {
    }
  }
}
