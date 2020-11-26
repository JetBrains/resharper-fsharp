using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl
{
  public static class OverridableMemberDeclarationUtil
  {
    public static bool IsOverride([NotNull] this IOverridableMemberDeclaration decl)
    {
      var tokenType = decl.MemberKeyword?.GetTokenType();
      if (tokenType == FSharpTokenType.OVERRIDE || tokenType == FSharpTokenType.DEFAULT)
        return true;

      return ObjExprNavigator.GetByMemberDeclaration(decl as IMemberDeclaration) != null;
    }
  }
}
