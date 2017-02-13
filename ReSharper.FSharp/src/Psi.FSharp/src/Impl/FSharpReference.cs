using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl
{
  public class FSharpReference : TreeReferenceBase<FSharpIdentifierToken>
  {
    [NotNull] private readonly FSharpSymbol mySymbol;

    public FSharpReference([NotNull] FSharpIdentifierToken owner, [NotNull] FSharpSymbol symbol) : base(owner)
    {
      mySymbol = symbol;
    }

    public override bool IsValid()
    {
      return myOwner.IsValid();
    }

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
      var fakeElement = new FSharpFakeElementFromReference(mySymbol, myOwner);
      return new ResolveResultWithInfo(new SimpleResolveResult(fakeElement), ResolveErrorType.OK);
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