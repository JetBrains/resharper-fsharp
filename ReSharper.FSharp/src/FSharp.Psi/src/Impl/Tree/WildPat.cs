using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class WildPat
  {
    public override string SourceName => "_";
    public override IFSharpIdentifierLikeNode NameIdentifier => null;

    public bool IsDeclaration => true;
    public IEnumerable<IDeclaration> Declarations => new[] {this};

    public override IType GetPatternType() => this.GetExpressionTypeFromFcs();
    public override IType Type => GetPatternType();

    public override TreeTextRange GetNameRange() => this.GetTreeTextRange();
  }
}
