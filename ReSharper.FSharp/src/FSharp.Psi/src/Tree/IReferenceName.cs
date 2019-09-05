using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IReferenceName
  {
    FSharpIdentifierToken Identifier { get; }
  }
}
