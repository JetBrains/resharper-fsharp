using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeParameterId
  {
    public string Name => Identifier.GetSourceName();
    public ITokenNode IdentifierToken => Identifier;
    public TreeTextRange NameRange => this.GetTreeTextRange();
  }
}
