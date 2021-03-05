using FSharp.Compiler.Symbols;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IInterfaceImplementation : INameIdentifierOwner
  {
    [CanBeNull] FSharpEntity FcsEntity { get; }
  }
}
