using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public abstract class FSharpReferenceBase : TreeReferenceBase<FSharpIdentifierToken>
  {
    protected FSharpReferenceBase([NotNull] FSharpIdentifierToken owner) : base(owner)
    {
    }

    public override bool IsValid()
    {
      return myOwner.IsValid();
    }

    public override string GetName()
    {
      return myOwner.GetText();
    }

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
    {
      throw new System.NotImplementedException();
    }

    public override TreeTextRange GetTreeTextRange()
    {
      return myOwner.GetTreeTextRange();
    }

    public override IReference BindTo(IDeclaredElement element)
    {
      // not supported yet
      return this;
    }

    public override IReference BindTo(IDeclaredElement element, ISubstitution substitution)
    {
      // not supported yet
      return this;
    }

    public override IAccessContext GetAccessContext()
    {
      return new DefaultAccessContext(myOwner);
    }
  }
}