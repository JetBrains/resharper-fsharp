using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public interface INameIdentifierOwner : IFSharpTreeNode
  {
    [CanBeNull] IFSharpIdentifierLikeNode NameIdentifier { get; }
  }
}
