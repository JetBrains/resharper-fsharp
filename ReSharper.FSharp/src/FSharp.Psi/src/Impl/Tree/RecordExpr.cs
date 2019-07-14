using FSharp.Compiler.SourceCodeServices;
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
    public override ITokenNode IdentifierToken => null;

    protected override FSharpSymbolReference CreateReference() =>
      new RecordCtorReference(this);
  }

  public class RecordCtorReference : FSharpSymbolReference
  {
    internal RecordCtorReference([NotNull] RecordExpr owner) : base(owner)
    {
    }

    public IRecordExpr RecordExpr => (myOwner as IRecordExpr).NotNull();

    public override bool HasMultipleNames => true;

    public override HybridCollection<string> GetAllNames() =>
      new HybridCollection<string>(RecordExpr.ExprBindings.Select(b => b.LongIdentifier?.Name).WhereNotNull());

    public override TreeOffset SymbolOffset
    {
      get
      {
        foreach (var binding in RecordExpr.ExprBindings)
          if (binding.LongIdentifier?.IdentifierToken is ITokenNode token)
            return token.GetTreeStartOffset();

        return TreeOffset.InvalidOffset;
      }
    }

    public override FSharpSymbol GetFSharpSymbol()
    {
      var symbolUse = GetSymbolUse();
      var field = symbolUse?.Symbol as FSharpField;
      return field?.DeclaringEntity?.Value;
    }

    public override string GetName() =>
      SharedImplUtil.MISSING_DECLARATION_NAME;

    public override TreeTextRange GetTreeTextRange() => myOwner.GetTreeTextRange();
  }
}
