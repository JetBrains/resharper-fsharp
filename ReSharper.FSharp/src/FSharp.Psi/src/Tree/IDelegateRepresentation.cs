using FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IDelegateRepresentation
  {
    FSharpEntity Delegate { get; }
    FSharpDelegateSignature DelegateSignature { get; }
  }
}
