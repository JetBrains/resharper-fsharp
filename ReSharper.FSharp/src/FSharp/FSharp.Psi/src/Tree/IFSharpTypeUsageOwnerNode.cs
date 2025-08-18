using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

public interface IFSharpTypeUsageOwnerNode : IFSharpTreeNode
{
  [CanBeNull] ITypeUsage TypeUsage { get; }

  [CanBeNull]
  ITypeUsage SetTypeUsage([NotNull] ITypeUsage typeUsage);
}
