using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface INamedModuleDeclaration
  {
    [NotNull] public string NamespaceName { get; }
  }
}
