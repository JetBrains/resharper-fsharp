using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class InterfaceImplementation
  {
    public IFSharpIdentifierLikeNode NameIdentifier => TypeName?.Identifier;
    public FSharpEntity FcsEntity => TypeName?.Reference.GetFSharpSymbol() as FSharpEntity;
  }
}
