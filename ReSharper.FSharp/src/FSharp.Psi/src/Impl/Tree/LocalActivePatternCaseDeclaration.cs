using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LocalActivePatternCaseDeclaration
  {
    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;
    public int Index => this.GetIndex();
  }
}
