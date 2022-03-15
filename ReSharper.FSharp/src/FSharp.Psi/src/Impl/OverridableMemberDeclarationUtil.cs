using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
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

    public static bool IsIndexer(this IMemberSignatureOrDeclaration decl) =>
      decl.SourceName == StandardMemberNames.DefaultIndexerName && decl.SourceName == decl.CompiledName;

    public static bool IsExplicitImplementation(this IOverridableMemberDeclaration memberDecl) =>
      InterfaceImplementationNavigator.GetByTypeMember(memberDecl) != null ||
      ObjExprNavigator.GetByMemberDeclaration(memberDecl) is {ArgExpression: null} ||
      ObjExprNavigator.GetByInterfaceMember(memberDecl) != null;
  }
}
