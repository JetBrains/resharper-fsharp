using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ActivePatternNamedCaseReferenceName
  {
    public override IFSharpIdentifier FSharpIdentifier => Identifier;
    public override FSharpReferenceContext? ReferenceContext => FSharpReferenceContext.Pattern;

    protected override FSharpSymbolReference CreateReference() =>
      new ActivePatternNamedCaseReferenceNameReference(this);

    public int Index => this.GetIndex();
  }

  internal class ActivePatternNamedCaseReferenceNameReference([NotNull] IFSharpReferenceOwner owner)
    : FSharpSymbolReference(owner)
  {
    public IActivePatternNamedCaseReferenceName ReferenceName =>
      (myOwner as IActivePatternNamedCaseReferenceName).NotNull();

    // todo: unify interfaces
    private static IFSharpReferenceOwner GetActivePatternReferenceOwner(IActivePatternId activePatternId) =>
      (IFSharpReferenceOwner) ReferenceExprNavigator.GetByIdentifier(activePatternId) ??
      ExpressionReferenceNameNavigator.GetByIdentifier(activePatternId);

    protected override IDeclaredElement GetDeclaredElement()
    {
      var element = base.GetDeclaredElement();
      if (element != null) return element;

      var referenceName = ReferenceName;
      var activePatternId = ActivePatternIdNavigator.GetByCase(referenceName);
      if (activePatternId == null) return null;

      var referenceOwner = GetActivePatternReferenceOwner(activePatternId);
      var declaredElement = referenceOwner?.Reference.Resolve().DeclaredElement;
      if (declaredElement == null) return null;

      var caseIndex = activePatternId.Cases.IndexOf(referenceName);

      foreach (var decl in declaredElement.GetDeclarations())
      {
        if (!(decl is IFSharpDeclaration fsDecl)) continue;

        var activePatternCase = fsDecl.GetActivePatternCaseByIndex(caseIndex);
        if (activePatternCase != null)
          return activePatternCase;
      }

      return null;
    }
  }
}
