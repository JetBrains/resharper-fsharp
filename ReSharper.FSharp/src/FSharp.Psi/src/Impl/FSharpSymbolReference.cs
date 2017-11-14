using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public class FSharpSymbolReference : TreeReferenceBase<FSharpIdentifierToken>
  {
    public FSharpSymbolReference([NotNull] FSharpIdentifierToken owner) : base(owner)
    {
    }

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
      if (!myOwner.IsValid())
        return ResolveResultWithInfo.Ignore;

      var psiModule = myOwner.GetPsiModule();
      var offset = myOwner.GetTreeStartOffset().Offset;
      var symbol = (myOwner.GetContainingFile() as IFSharpFile)?.GetSymbolUse(offset);

      var element = symbol != null
        ? FSharpElementsUtil.GetDeclaredElement(symbol, psiModule, myOwner)
        : null;
      return element != null
        ? new ResolveResultWithInfo(new SimpleResolveResult(element), ResolveErrorType.OK) // todo: add substitutions
        : ResolveResultWithInfo.Ignore;
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
      // not supported yet (called during refactorings)
      return this;
    }

    public override IReference BindTo(IDeclaredElement element, ISubstitution substitution)
    {
      // not supported yet (called during refactorings)
      return this;
    }

    public override IAccessContext GetAccessContext()
    {
      return new DefaultAccessContext(myOwner);
    }
  }
}