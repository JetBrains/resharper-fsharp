using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Psi.FSharp.Tree
{
  public partial interface IModuleOrNamespaceDeclaration : ICachedDeclaration2
  {
    string ShortName { get; }
  }
}