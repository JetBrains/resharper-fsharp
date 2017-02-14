using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl
{
  public class FSharpResolvedReference : FSharpReferenceBase
  {
    [NotNull] private readonly FSharpSymbol mySymbol;

    public FSharpResolvedReference([NotNull] FSharpIdentifierToken owner, [NotNull] FSharpSymbol symbol) : base(owner)
    {
      mySymbol = symbol;
    }

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
      var fakeElement = new FSharpFakeElementFromReference(mySymbol, myOwner);
      return new ResolveResultWithInfo(new SimpleResolveResult(fakeElement), ResolveErrorType.OK);
    }
  }
}