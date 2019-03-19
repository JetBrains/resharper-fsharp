using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public interface IReferenceExpression : IFSharpTreeNode
  {
    FSharpSymbolReference Reference { get; set; }

    [CanBeNull] ITokenNode IdentifierToken { get; }

    [NotNull]
    IReferenceExpression SetName([NotNull] string name);
  }
}
