namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class ObjExprNavigator
  {
    public static IObjExpr GetByMember(IOverridableMemberDeclaration param) =>
      GetByMemberDeclaration(param) ?? GetByInterfaceMember(param);
  }
}
