using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class RecordExpr
  {
    public FSharpSymbolReference Reference { get; private set; }
    public IFSharpIdentifier FSharpIdentifier => null;

    public IFSharpReferenceOwner SetName(string name) => this;

    protected override void PreInit()
    {
      base.PreInit();
      Reference = new RecordCtorReference(this);
    }

    public override ReferenceCollection GetFirstClassReferences() =>
      new ReferenceCollection(Reference);
  }

  public class RecordCtorReference : FSharpSymbolReference
  {
    internal RecordCtorReference([NotNull] RecordExpr owner) : base(owner)
    {
    }

    public IRecordExpr RecordExpr => (myOwner as IRecordExpr).NotNull();

    public override bool HasMultipleNames => true;

    public override HybridCollection<string> GetAllNames() =>
      new HybridCollection<string>(RecordExpr.FieldBindings.Select(b => b.ReferenceName?.ShortName).WhereNotNull());

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

    public override FSharpSymbol GetFSharpSymbol()
    {
      var symbolUse = GetSymbolUse();
      if (symbolUse?.Symbol is FSharpField field)
        return field.DeclaringEntity?.Value;

      var functionExpr = GetFunctionExpression(RecordExpr);
      var binding = BindingNavigator.GetByExpression(functionExpr);
      if (binding == null || !(binding.HeadPattern is INamedPat namedPat))
        return null;

      var mfv = namedPat.GetFSharpSymbol() as FSharpMemberOrFunctionOrValue;
      var returnParameterType = mfv?.ReturnParameter.Type;
      if (returnParameterType == null || !returnParameterType.HasTypeDefinition)
        return null;

      var entity = returnParameterType.TypeDefinition;
      return entity.IsFSharpRecord ? entity : null;
    }

    private static IFSharpExpression GetFunctionExpression(IFSharpExpression expression)
    {
      IFSharpExpression currentExpression = IfExprNavigator.GetByThenExpr(expression);
      currentExpression ??= IfExprNavigator.GetByElseExpr(expression);
      var matchClause = MatchClauseNavigator.GetByExpression(expression);
      currentExpression = matchClause == null ? currentExpression : MatchExprNavigator.GetByClause(matchClause);
      currentExpression ??= MatchLambdaExprNavigator.GetByClause(matchClause);

      var sequentialExpr = SequentialExprNavigator.GetByExpression(expression);
      currentExpression ??= sequentialExpr;
      if (sequentialExpr != null && sequentialExpr.Expressions.Last() != expression)
        return null;

      if (currentExpression == null)
        return expression;

      return GetFunctionExpression(currentExpression);
    }

    public override string GetName() =>
      SharedImplUtil.MISSING_DECLARATION_NAME;

    public override TreeTextRange GetTreeTextRange() => myOwner.GetTreeTextRange();
  }
}
