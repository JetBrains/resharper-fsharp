using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class RecordExpr
  {
    public override IFSharpIdentifier FSharpIdentifier => null;

    public override IFSharpReferenceOwner SetName(string name) => this;

    protected override FSharpSymbolReference CreateReference() =>
      new RecordCtorReference(this);
  }

  public class RecordCtorReference : FSharpSymbolReference
  {
    internal RecordCtorReference([NotNull] IRecordExpr owner) : base(owner)
    {
    }

    public IRecordExpr RecordExpr => (myOwner as IRecordExpr).NotNull();

    public override bool HasMultipleNames => true;

    public override HybridCollection<string> GetAllNames() =>
      new(RecordExpr.FieldBindings.Select(b => b.ReferenceName?.ShortName).WhereNotNull());

    public override TreeOffset SymbolOffset
    {
      get
      {
        foreach (var binding in RecordExpr.FieldBindings)
          if (binding.ReferenceName?.Identifier is ITokenNode token)
            return token.GetTreeStartOffset();

        return TreeOffset.InvalidOffset;
      }
    }

    // todo: unify with FSharpPatternUtil impl
    private static IFSharpPattern IgnoreInnerAsPatsToRight(IFSharpPattern pattern) => 
      pattern is IAsPat asPat ? IgnoreInnerAsPatsToRight(asPat.RightPattern) : pattern;

    public override FSharpSymbol GetFcsSymbol()
    {
      var symbolUse = GetSymbolUse();
      if (symbolUse?.Symbol is FSharpField field)
        return field.DeclaringEntity?.Value;

      // todo: cover other contexts
      var sequentialExpr = SequentialExprNavigator.GetByExpression(RecordExpr.IgnoreParentParens());
      if (sequentialExpr != null && sequentialExpr.Expressions.Last() != RecordExpr)
        return null;
      var exprToGetBy = sequentialExpr ?? RecordExpr.IgnoreParentParens();

      var binding = BindingNavigator.GetByExpression(exprToGetBy);
      if (binding == null) return null;

      var headPattern = IgnoreInnerAsPatsToRight(binding.HeadPattern);
      if (!(headPattern is IReferencePat refPat))
        return null;

      var mfv = refPat.GetFcsSymbol() as FSharpMemberOrFunctionOrValue;
      var returnParameterType = mfv?.ReturnParameter.Type;
      if (!(returnParameterType is { HasTypeDefinition: true }))
        return null;

      var entity = returnParameterType.TypeDefinition;
      return entity.IsFSharpRecord ? entity : null;
    }

    public override string GetName() =>
      SharedImplUtil.MISSING_DECLARATION_NAME;

    public override TreeTextRange GetTreeTextRange() => myOwner.GetTreeTextRange();
  }
}
