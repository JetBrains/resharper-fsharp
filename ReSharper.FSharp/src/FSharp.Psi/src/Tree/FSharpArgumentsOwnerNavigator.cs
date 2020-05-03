using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public class FSharpArgumentOwnerNavigator
  {
    public static IFSharpArgumentsOwner GetByArgumentExpression([CanBeNull] IFSharpExpression param) =>
      (IFSharpArgumentsOwner)AppLikeExprNavigator.GetByArgumentExpression(param) ??
      AttributeNavigator.GetByExpression(param);
  }
}
