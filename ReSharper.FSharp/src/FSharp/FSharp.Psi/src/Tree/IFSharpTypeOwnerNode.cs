using FSharp.Compiler.Symbols;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

public interface IFSharpTypeOwnerNode : IFSharpTreeNode
{
  // todo
  // [CanBeNull] FSharpType FcsType { get; }
}
