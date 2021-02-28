using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LocalBinding
  {
    public TreeNodeCollection<IAttribute> AllAttributes => Attributes;
    public bool IsMutable => MutableKeyword != null;

    public void SetIsMutable(bool value)
    {
      if (!value)
        throw new System.NotImplementedException();

      var headPat = HeadPattern;
      if (headPat != null)
        FSharpImplUtil.AddTokenBefore(headPat, FSharpTokenType.MUTABLE);
    }

    public bool HasParameters => !ParametersDeclarationsEnumerable.IsEmpty();
  }
}
