using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NamedModuleDeclaration
  {
    protected override string DeclaredElementName => Identifier.GetModuleCompiledName(Attributes);
    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;
  }
}
