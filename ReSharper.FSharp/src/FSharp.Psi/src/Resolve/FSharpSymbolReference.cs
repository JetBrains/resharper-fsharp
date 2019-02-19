using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Naming;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class FSharpSymbolReference : TreeReferenceBase<FSharpIdentifierToken>
  {
    public FSharpSymbolReference([NotNull] FSharpIdentifierToken owner) : base(owner)
    {
    }

    public FSharpSymbolUse GetSymbolUse() =>
      (myOwner.GetContainingFile() as IFSharpFile)?.GetSymbolUse(myOwner.GetTreeStartOffset().Offset);

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
      if (!myOwner.IsValid())
        return ResolveResultWithInfo.Ignore;

      var symbol = GetSymbolUse()?.Symbol;
      var element = symbol != null
        ? FSharpElementsUtil.GetDeclaredElement(symbol, myOwner.GetPsiModule(), myOwner)
        : null;

      return element != null
        ? new ResolveResultWithInfo(new SimpleResolveResult(element), ResolveErrorType.OK) // todo: add substitutions
        : ResolveResultWithInfo.Ignore;
    }

    public override string GetName() => myOwner.GetText();

    public override bool IsValid() =>
      base.IsValid() && myOwner.SymbolReference == this;

    public override TreeTextRange GetTreeTextRange() =>
      myOwner.GetTreeTextRange();

    public override IAccessContext GetAccessContext() =>
      new DefaultAccessContext(myOwner);

    public override IReference BindTo(IDeclaredElement element)
    {
      // Disable for non-F# elements until such refactorings are possible.
      if (!(element is IFSharpDeclaredElement fsElement))
        return this;

      using (WriteLockCookie.Create(myOwner.IsPhysical()))
      {
        var name = NamingManager.GetNamingLanguageService(myOwner.Language).MangleNameIfNecessary(fsElement.SourceName);
        var newToken = new FSharpIdentifierToken(name);
        LowLevelModificationUtil.ReplaceChildRange(myOwner, myOwner, newToken);
        var newReference = new FSharpSymbolReference(newToken);

        // We have to change the reference so it resolves to the new element.
        // We don't, however, want to actually resolve it and to wait for FCS to type check all the needed projects
        // so we set a fake resolve result beforehand.
        ResolveUtil.SetFakedResolveTo(newReference, element, null);
        newToken.SymbolReference = newReference;
        return newReference;
      }
    }

    public override IReference BindTo(IDeclaredElement element, ISubstitution substitution)
    {
      // Not yet supported (called during refactorings).
      return this;
    }

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName) =>
      throw new System.NotImplementedException();
  }
}
