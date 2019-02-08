using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NestedTypeUnionCaseDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetSourceName();
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;
  }
}
