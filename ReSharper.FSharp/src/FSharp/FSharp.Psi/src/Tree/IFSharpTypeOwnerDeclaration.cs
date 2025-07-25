using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

public interface IFSharpTypeOwnerDeclaration : IFSharpDeclaration
{
  [CanBeNull] ITypeUsage TypeUsage { get; }

  [CanBeNull]
  ITypeUsage SetTypeUsage([NotNull] ITypeUsage typeUsage);
}
