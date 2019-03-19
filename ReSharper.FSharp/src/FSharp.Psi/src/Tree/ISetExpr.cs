using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface ISetExpr
  {
    [CanBeNull]
    ITokenNode ReferenceIdentifier { get; }
  }
}
