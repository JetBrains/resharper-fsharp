using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NestedTypeUnionCaseDeclaration
  {
    protected override string DeclaredElementName => Identifier.GetCompiledName(Attributes);
    public override IFSharpIdentifier NameIdentifier => Identifier;
  }
}
