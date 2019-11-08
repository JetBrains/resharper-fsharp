using FSharp.Compiler.SourceCodeServices;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class RecordFieldDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetSourceName();
    public override IFSharpIdentifierLikeNode NameIdentifier => (IFSharpIdentifierLikeNode) Identifier;

    protected override IDeclaredElement CreateDeclaredElement() =>
      GetFSharpSymbol() is FSharpField field ? new FSharpRecordField(this, field) : null;

    public bool IsMutable => MutableKeyword != null;

    public void SetIsMutable(bool value) =>
      throw new System.NotImplementedException();
  }
}
