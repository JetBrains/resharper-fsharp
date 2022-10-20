using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class RecordFieldDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetSourceName();
    public override IFSharpIdentifierLikeNode NameIdentifier => (IFSharpIdentifierLikeNode) Identifier;

    protected override IDeclaredElement CreateDeclaredElement() =>
      new FSharpRecordField(this);

    public bool IsMutable => MutableKeyword != null;

    public void SetIsMutable(bool value)
    {
      if (value == IsMutable)
        return;

      if (!value && MutableKeyword != null)
      {
        if (MutableKeyword.NextSibling.IsWhitespaceToken())
        {
          ModificationUtil.DeleteChild(MutableKeyword.NextSibling);
        }
        ModificationUtil.DeleteChild(MutableKeyword);
        return;
      }

      var identifier = Identifier;
      if (identifier != null)
        FSharpImplUtil.AddTokenBefore(identifier, FSharpTokenType.MUTABLE);
    }
  }
}
