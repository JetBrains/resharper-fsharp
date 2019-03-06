using JetBrains.Annotations;
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
  public class FSharpSymbolReference : TreeReferenceBase<IReferenceExpression>
  {
    public FSharpSymbolReference([NotNull] IReferenceExpression owner) : base(owner)
    {
    }

    public FSharpSymbolUse GetSymbolUse() =>
      myOwner.IdentifierToken is var token && token != null
        ? myOwner.FSharpFile.GetSymbolUse(token.GetTreeStartOffset().Offset)
        : null;

    public virtual FSharpSymbol GetFSharpSymbol() =>
      GetSymbolUse()?.Symbol;

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
      if (!myOwner.IsValid())
        return ResolveResultWithInfo.Ignore;

      var symbol = GetFSharpSymbol();
      var element = symbol != null
        ? FSharpElementsUtil.GetDeclaredElement(symbol, myOwner.GetPsiModule(), myOwner)
        : null;

      return element != null
        ? new ResolveResultWithInfo(new SimpleResolveResult(element), ResolveErrorType.OK) // todo: add substitutions
        : ResolveResultWithInfo.Ignore;
    }

    public override string GetName() =>
      myOwner.IdentifierToken?.GetText() ??
      SharedImplUtil.MISSING_DECLARATION_NAME;

    public override bool IsValid() =>
      base.IsValid() && myOwner.Reference == this;

    public override TreeTextRange GetTreeTextRange() =>
      myOwner.IdentifierToken?.GetTreeTextRange() ??
      TreeTextRange.InvalidRange;

    public override IAccessContext GetAccessContext() =>
      new DefaultAccessContext(myOwner);

    public override IReference BindTo(IDeclaredElement element)
    {
      // Disable for non-F# elements until such refactorings are possible.
      if (!(element is IFSharpDeclaredElement fsElement))
        return this;

      var sourceName = fsElement.SourceName;
      if (sourceName == SharedImplUtil.MISSING_DECLARATION_NAME)
        return this;

      var newName = GetReferenceName(sourceName);
      using (WriteLockCookie.Create(myOwner.IsPhysical()))
      {
        var name = NamingManager.GetNamingLanguageService(myOwner.Language).MangleNameIfNecessary(newName);
        var newExpression = myOwner.SetName(name);
        return newExpression.Reference;
      }
    }

    protected virtual string GetReferenceName([NotNull] string name) => name;

    public override IReference BindTo(IDeclaredElement element, ISubstitution substitution)
    {
      // No actual binding, not supported yet.
      return this;
    }

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName) =>
      throw new System.NotImplementedException();
  }
}
