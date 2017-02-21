using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Psi.FSharp.Tree
{
  public partial interface IModuleLikeDeclaration : ICachedDeclaration2
  {
    bool IsModule { get; }
  }
}