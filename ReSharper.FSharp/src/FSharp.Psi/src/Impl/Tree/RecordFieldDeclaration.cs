using JetBrains.Diagnostics;
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

      if (value)
      {
        Identifier.AddTokenBefore(FSharpTokenType.MUTABLE);
        return;
      }

      var mutableKeyword = MutableKeyword.NotNull();
      var firstMeaningfulToken = FirstChild?.GetNextMeaningfulToken(true);
      if (mutableKeyword == firstMeaningfulToken && Identifier is { PrevSibling: { } identifierPrevSibling })
        ModificationUtil.DeleteChildRange(mutableKeyword, identifierPrevSibling);
      else
      {
        if (mutableKeyword.NextSibling is { } mutableNextSibling &&
            mutableNextSibling.GetTokenType() == FSharpTokenType.WHITESPACE)
          ModificationUtil.DeleteChild(mutableNextSibling);

        ModificationUtil.DeleteChild(mutableKeyword);
      }
    }
  }
}
