using System;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class FSharpSymbolReference : TreeReferenceBase<IFSharpReferenceOwner>
  {
    public FSharpSymbolReference([NotNull] IFSharpReferenceOwner owner) : base(owner)
    {
    }

    public FSharpSymbolUse GetSymbolUse() =>
      SymbolOffset is var offset && offset.IsValid()
        ? myOwner.FSharpFile.GetSymbolUse(offset.Offset)
        : null;

    public virtual TreeOffset SymbolOffset =>
      myOwner.IdentifierToken is var token && token != null
        ? token.GetTreeStartOffset()
        : TreeOffset.InvalidOffset;

    public virtual FSharpSymbol GetFSharpSymbol() =>
      GetSymbolUse()?.Symbol;

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
      if (!myOwner.IsValid())
        return ResolveResultWithInfo.Ignore;

      var symbol = GetFSharpSymbol();
      var element = symbol?.GetDeclaredElement(myOwner.GetPsiModule(), myOwner);

      return element != null
        ? new ResolveResultWithInfo(new SimpleResolveResult(element), ResolveErrorType.OK) // todo: add substitutions
        : ResolveResultWithInfo.Ignore;
    }

    public override string GetName() =>
      myOwner.IdentifierToken.GetSourceName();

    public override bool HasMultipleNames =>
      AttributeNavigator.GetByReferenceName(myOwner as ITypeReferenceName) != null;

    public override HybridCollection<string> GetAllNames()
    {
      var name = GetName();
      return HasMultipleNames
        ? new HybridCollection<string>(name, name + "Attribute")
        : new HybridCollection<string>(name);
    }

    public override TreeTextRange GetTreeTextRange() =>
      myOwner.IdentifierToken?.GetTreeTextRange() ??
      TreeTextRange.InvalidRange;

    public override IAccessContext GetAccessContext() =>
      new DefaultAccessContext(myOwner);

    public override IReference BindTo(IDeclaredElement element)
    {
      // Disable for non-F# elements until such refactorings are possible.
      if (!CanBindTo(element))
        return this;

      var sourceName = element.GetSourceName();
      if (sourceName == SharedImplUtil.MISSING_DECLARATION_NAME)
        return this;

      using (WriteLockCookie.Create(myOwner.IsPhysical()))
        return myOwner.SetName(FSharpBindingUtil.SuggestShortReferenceName(this, element)).Reference;
    }

    private static bool CanBindTo(IDeclaredElement element) =>
      element is IFSharpDeclaredElement || element is ITypeParameter;

    public override IReference BindTo(IDeclaredElement element, ISubstitution substitution)
    {
      // No actual binding, not supported yet.
      return this;
    }

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName) =>
      throw new NotImplementedException();

    public bool IsQualified =>
      GetElement() switch
      {
        IReferenceExpr referenceExpr => referenceExpr.Qualifier != null,
        IReferenceName referenceName => referenceName.Qualifier != null,
        _ => false
      };
  }
}
