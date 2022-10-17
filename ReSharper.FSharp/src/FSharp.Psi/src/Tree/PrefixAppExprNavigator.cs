using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class PrefixAppExprNavigator
  {
    [CanBeNull]
    public static IPrefixAppExpr GetByExpression([CanBeNull] IFSharpExpression param) =>
      GetByFunctionExpression(param) ??
      GetByArgumentExpression(param);
  }
  
  // public partial class FSharpParameterOwnerDeclarationNavigator
  // {
  //   IFSharpParameterOwnerDeclaration GetBy
  // }
}
