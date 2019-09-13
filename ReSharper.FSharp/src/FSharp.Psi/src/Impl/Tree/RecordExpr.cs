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
    public FSharpSymbolReference Reference { get; private set; }
    public ITokenNode IdentifierToken => null;

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
      new HybridCollection<string>(RecordExpr.ExprBindings.Select(b => b.ReferenceName?.ShortName).WhereNotNull());

    public override TreeOffset SymbolOffset
    {
      get
      {
        foreach (var binding in RecordExpr.ExprBindings)
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

      // todo: cover other contexts
      var binding = BindingNavigator.GetByExpression(RecordExpr.IgnoreParentParens());
      if (binding == null || !(binding.HeadPattern is INamedPat namedPat))
        return null;

      var mfv = namedPat.GetFSharpSymbol() as FSharpMemberOrFunctionOrValue;
      var fsType = mfv?.FullType;
      if (fsType == null || !fsType.HasTypeDefinition)
        return null;

      var entity = fsType.TypeDefinition;
      return entity.IsFSharpRecord ? entity : null;
    }

    public override string GetName() =>
      SharedImplUtil.MISSING_DECLARATION_NAME;

    public override TreeTextRange GetTreeTextRange() => myOwner.GetTreeTextRange();
  }
}
