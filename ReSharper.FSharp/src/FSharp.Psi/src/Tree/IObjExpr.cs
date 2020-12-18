using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IObjExpr : IFSharpTypeElementDeclaration
  {
    [CanBeNull] FSharpEntity FcsEntity { get; }
  }
}
