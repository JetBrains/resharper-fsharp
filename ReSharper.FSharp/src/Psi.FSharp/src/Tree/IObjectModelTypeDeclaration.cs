using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IObjectModelTypeDeclaration
  {
    FSharpPartKind TypePartKind { get; }
  }
}