using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LocalBinding
  {
    public bool IsMutable => MutableKeyword != null;

    public void SetIsMutable(bool value)
    {
      if (!value)
        throw new System.NotImplementedException();

      var headPat = HeadPattern;
      if (headPat != null)
        headPat.AddTokenBefore(FSharpTokenType.MUTABLE);
    }

    public bool IsInline => InlineKeyword != null;

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


    public bool HasParameters => !ParametersDeclarationsEnumerable.IsEmpty();
  }
}
