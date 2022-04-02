using FSharp.Compiler.Symbols;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IDelegateRepresentation
  {
    [CanBeNull] FSharpEntity Delegate { get; }
    [CanBeNull] FSharpDelegateSignature DelegateSignature { get; }
  }
}
