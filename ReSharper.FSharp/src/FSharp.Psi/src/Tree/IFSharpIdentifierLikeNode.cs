using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  // todo: remove this interface, keep IFSharpIdentifier
  public interface IFSharpIdentifierLikeNode : IIdentifier
  {
    ITokenNode IdentifierToken { get; }
    TreeTextRange NameRange { get; }
  }
}
