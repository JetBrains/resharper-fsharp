using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LetBindingsDeclaration
  {
    public IBinding FirstBinding => BindingsEnumerable.FirstOrDefault();
    public ITokenNode BindingKeyword => FirstBinding?.BindingKeyword;

    public bool IsRecursive => FirstBinding?.RecKeyword != null;

    public bool IsUse => BindingKeyword?.GetTokenType() == FSharpTokenType.USE;

    public void SetIsRecursive(bool value)
    {
      if (!value)
        throw new System.NotImplementedException();

      using var _ = WriteLockCookie.Create(IsPhysical());

      FirstBinding.NotNull().BindingKeyword.AddTokenAfter(FSharpTokenType.REC);
    }
  }
}
