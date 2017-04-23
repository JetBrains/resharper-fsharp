using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations;

namespace JetBrains.ReSharper.Psi.FSharp.Tree
{
  public partial interface IFSharpObjectModelTypeDeclaration
  {
    FSharpPartKind TypePartKind { get; }
  }
}