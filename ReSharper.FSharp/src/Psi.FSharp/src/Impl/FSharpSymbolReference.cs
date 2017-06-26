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
  public class FSharpSymbolReference : FSharpReferenceBase
  {
    private readonly bool myLazyResolve;

    public FSharpSymbolReference([NotNull] FSharpIdentifierToken owner, bool lazyResolve) : base(owner)
    {
      myLazyResolve = lazyResolve;
    }

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
      if (!myOwner.IsValid())
        return ResolveResultWithInfo.Ignore;

      var psiModule = myOwner.GetPsiModule();
      var fsFile = myOwner.GetContainingFile() as IFSharpFile;
      var symbol = myLazyResolve ? FindFSharpSymbol(fsFile) : myOwner.FSharpSymbol;

      var element = symbol != null ? FSharpElementsUtil.GetDeclaredElement(symbol, psiModule, myOwner) : null;
      return element != null
        ? new ResolveResultWithInfo(new SimpleResolveResult(element), ResolveErrorType.OK)
        : ResolveResultWithInfo.Ignore;
    }

    [CanBeNull]
    private FSharpSymbol FindFSharpSymbol(IFSharpFile fsFile)
    {
      return fsFile != null
        ? FSharpSymbolsUtil.TryFindFSharpSymbol(fsFile, myOwner.GetText(), myOwner.GetTreeEndOffset().Offset)
        : null;
    }
  }
}