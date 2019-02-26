using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IDelegateDeclaration
  {
    FSharpEntity Delegate { get; }
    FSharpDelegateSignature DelegateSignature { get; }
  }
}
