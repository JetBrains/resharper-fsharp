using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2;

namespace JetBrains.ReSharper.Psi.FSharp.Tree
{
  public partial interface IObjectModelTypeDeclaration
  {
    FSharpPartKind TypePartKind { get; }
  }
}