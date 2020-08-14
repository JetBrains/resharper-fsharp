using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LetBindingsDeclaration
  {
    public bool IsRecursive => RecKeyword != null;
    public bool IsUse => BindingKeyword?.GetTokenType() == FSharpTokenType.USE;

    public void SetIsRecursive(bool value)
    {
      if (!value)
        throw new System.NotImplementedException();

      BindingKeyword.NotNull().AddTokenAfter(FSharpTokenType.REC);
    }

    public void SetIsInline(bool value)
    {
      if (value)
        throw new System.NotImplementedException();

      using var _ = WriteLockCookie.Create(IsPhysical());
      var inlineKeyword = InlineKeyword;
      if (inlineKeyword.PrevSibling is Whitespace whitespace)
        ModificationUtil.DeleteChildRange(whitespace, inlineKeyword);
      else
        ModificationUtil.DeleteChild(inlineKeyword);
    }
  }
}
