using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public interface IFSharpIdentifierLikeNode : IIdentifier
  {
    ITokenNode IdentifierToken { get; }
  }
}
