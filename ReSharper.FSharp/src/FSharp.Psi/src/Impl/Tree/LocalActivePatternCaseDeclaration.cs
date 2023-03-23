using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class LocalActivePatternCaseDeclaration
  {
    public override IFSharpIdentifier NameIdentifier => Identifier;
    public int Index => this.GetIndex();
  }
}
