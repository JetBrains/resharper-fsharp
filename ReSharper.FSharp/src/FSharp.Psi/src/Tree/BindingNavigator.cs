using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class BindingNavigator
  {
    [CanBeNull]
    public static IBinding GetByHeadPattern([CanBeNull] IFSharpPattern param) =>
      (IBinding) BindingImplementationNavigator.GetByHeadPattern(param) ??
      BindingSignatureNavigator.GetByHeadPattern(param as IReferencePat);
  }
}
