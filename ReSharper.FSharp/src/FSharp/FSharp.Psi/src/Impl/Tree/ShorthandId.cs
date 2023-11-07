using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ShorthandId
  {
    public override string SourceName => "_";
    public string Name => SourceName;
    public override IFSharpIdentifier NameIdentifier => this;
    public ITokenNode IdentifierToken => Underscore;
    public override TreeTextRange GetNameRange() => this.GetTreeTextRange();
    public TreeTextRange NameRange => GetNameRange();
    public override IType Type => this.GetExpressionTypeFromFcs();
  }
}
