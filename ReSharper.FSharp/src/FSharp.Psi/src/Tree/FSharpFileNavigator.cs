using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial class FSharpFileNavigator
  {
    [CanBeNull] public static IFSharpFile GetByModuleDeclaration([CanBeNull] IModuleLikeDeclaration param) =>
      (IFSharpFile)FSharpImplFileNavigator.GetByModuleDeclaration(param) ??
      FSharpSigFileNavigator.GetByModuleDeclaration(param as ITopLevelModuleLikeDeclaration);
  }
}
