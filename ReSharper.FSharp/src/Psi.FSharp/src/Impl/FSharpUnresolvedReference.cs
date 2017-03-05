using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl
{
  public class FSharpUnresolvedReference : FSharpReferenceBase
  {
    public FSharpUnresolvedReference([NotNull] FSharpIdentifierToken owner) : base(owner)
    {
    }

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
      var symbol = FindFSharpSymbol();
      if (symbol == null) return ResolveResultWithInfo.Ignore;

      var fakeElement = new FSharpFakeElementFromReference(symbol, myOwner);
      return new ResolveResultWithInfo(new SimpleResolveResult(fakeElement), ResolveErrorType.OK);
    }

    [CanBeNull]
    private FSharpSymbol FindFSharpSymbol()
    {
      var fsFile = myOwner.GetContainingFile() as IFSharpFile;
      return fsFile != null
        ? FSharpSymbolsUtil.TryFindFSharpSymbol(fsFile, myOwner.GetText(), myOwner.GetTreeEndOffset().Offset)
        : null;
    }
  }
}