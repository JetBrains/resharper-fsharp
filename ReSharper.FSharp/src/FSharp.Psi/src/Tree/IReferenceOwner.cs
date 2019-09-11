using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public interface IReferenceOwner : IFSharpTreeNode
  {
    FSharpSymbolReference Reference { get; }

    [CanBeNull] ITokenNode IdentifierToken { get; }

    [NotNull]
    IReferenceOwner SetName([NotNull] string name);
  }
}
