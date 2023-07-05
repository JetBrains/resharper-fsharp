using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class BindingSignature
  {
    public bool IsMutable => MutableKeyword != null;

    public void SetIsMutable(bool value)
    {
      if (!value)
      {
        if (MutableKeyword != null)
        {
          if (MutableKeyword.PrevSibling is Whitespace whitespace)
          {
            ModificationUtil.DeleteChildRange(whitespace, MutableKeyword);
          }
          else
          {
            ModificationUtil.DeleteChild(MutableKeyword);
          }
        }
        return;
      }

      var headPat = HeadPattern;
      if (headPat != null)
        FSharpImplUtil.AddTokenBefore(headPat, FSharpTokenType.MUTABLE);
    }
  }
}
