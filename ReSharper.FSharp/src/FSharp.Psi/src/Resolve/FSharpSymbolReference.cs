using System;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.Shim.AssemblyReader;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util.DataStructures;
using Microsoft.FSharp.Core;

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

    public virtual TreeOffset SymbolOffset
    {
      get
      {
        if (myOwner.FSharpIdentifier is { } fsIdentifier)
          return fsIdentifier.NodeType == FSharpTokenType.LPAREN_STAR_RPAREN
            ? fsIdentifier.GetTreeStartOffset()
            : fsIdentifier.NameRange.StartOffset;

        return myOwner.GetTreeStartOffset();
      }
    }

    public virtual FSharpSymbol GetFcsSymbol() =>
      GetSymbolUse()?.Symbol;

    public bool HasFcsSymbol => GetSymbolUse() != null;

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
      if (!myOwner.IsValid())
        return ResolveResultWithInfo.Ignore;

      var element = GetDeclaredElement();

      return element != null
        ? new ResolveResultWithInfo(new SimpleResolveResult(element), ResolveErrorType.OK) // todo: add substitutions
        : ResolveResultWithInfo.Ignore;
    }

    protected virtual IDeclaredElement GetDeclaredElement()
    {
      var symbol = GetFcsSymbol();
      return symbol?.GetDeclaredElement(myOwner.GetPsiModule(), myOwner);
    }

    public override string GetName() =>
      myOwner.FSharpIdentifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;

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
      myOwner.FSharpIdentifier?.NameRange ?? TreeTextRange.InvalidRange;

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
        return myOwner.SetName(FSharpReferenceBindingUtil.SuggestShortReferenceName(this, element)).Reference;
    }

    private static bool CanBindTo(IDeclaredElement element) =>
      element.PresentationLanguage.Is<FSharpLanguage>() ||

      element.GetSolution().GetComponent<IFcsAssemblyReaderShim>().IsEnabled &&
      AssemblyReaderShimModule.SupportedLanguages.Contains(element.PresentationLanguage);

    public override IReference BindTo(IDeclaredElement element, ISubstitution substitution)
    {
      // No actual binding, not supported yet.
      return this;
    }

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName) =>
      throw new NotImplementedException();

    public bool IsQualified =>
      GetElement() is IFSharpQualifiableReferenceOwner { IsQualified: true };

    public FSharpSymbolReference QualifierReference =>
      GetElement() is IFSharpQualifiableReferenceOwner referenceOwner ? referenceOwner.QualifierReference : null;

    public void SetQualifier([NotNull] IClrDeclaredElement declaredElement)
    {
      if (GetElement() is IFSharpQualifiableReferenceOwner referenceOwner)
        referenceOwner.SetQualifier(declaredElement);
    }

    /// Does not reuse existing file resolve results, does complete lookup by name.
    public FSharpOption<FSharpSymbolUse> ResolveWithFcs([NotNull] string opName, bool resolveExpr, bool qualified)
    {
      var referenceOwner = GetElement();
      var checkerService = referenceOwner.CheckerService;

      var names = qualified && referenceOwner is IFSharpQualifiableReferenceOwner qualifiableReferenceOwner
        ? qualifiableReferenceOwner.Names
        : new[] {GetName()};

      return checkerService.ResolveNameAtLocation(referenceOwner.FSharpIdentifier, names, resolveExpr, opName);
    }
  }
}
