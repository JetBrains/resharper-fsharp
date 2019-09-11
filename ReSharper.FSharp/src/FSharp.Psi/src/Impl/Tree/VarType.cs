using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class VarType
  {
    public override ITokenNode IdentifierToken => Identifier;

    protected override FSharpSymbolReference CreateReference() =>
      new VarTypeReference(this);
  }

  internal class VarTypeReference : FSharpSymbolReference
  {
    public VarTypeReference([NotNull] IReferenceOwner owner) : base(owner)
    {
    }

    public override TreeOffset SymbolOffset =>
      myOwner.GetTreeStartOffset();

    public override string GetName() =>
      myOwner.IdentifierToken.GetSourceName();

    public override TreeTextRange GetTreeTextRange() =>
      myOwner.GetTreeTextRange();
  }
}
