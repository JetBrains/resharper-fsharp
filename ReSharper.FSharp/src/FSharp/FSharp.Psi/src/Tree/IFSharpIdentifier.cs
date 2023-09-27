using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IFSharpIdentifier : IIdentifier
  {
    [CanBeNull] ITokenNode IdentifierToken { get; }
    TreeTextRange NameRange { get; }
  }
}
