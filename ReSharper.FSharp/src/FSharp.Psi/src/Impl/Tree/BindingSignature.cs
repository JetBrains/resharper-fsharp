using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi;
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
          ModificationUtil.DeleteChild(MutableKeyword);
        }
        return;
      }

      var headPat = HeadPattern;
      if (headPat != null)
        FSharpImplUtil.AddTokenBefore(headPat, FSharpTokenType.MUTABLE);
    }

    public void SetAccessModifier(AccessRights accessModifier)
    {
      // TODO: check for AccessRights.NONE
      if (AccessModifier == null)
      {
        if (Attributes.IsEmpty && FirstChild != null)
        {
          ModificationUtil.AddChildBefore(FirstChild, ModifiersUtil.GetAccessNode(accessModifier));
        }
        else
        {
          ModificationUtil.AddChildAfter(Attributes.Last(), ModifiersUtil.GetAccessNode(accessModifier));
        }
      }
      else
      {
        ModificationUtil.ReplaceChild(AccessModifier, ModifiersUtil.GetAccessNode(accessModifier));
      }
    }
  }
}
