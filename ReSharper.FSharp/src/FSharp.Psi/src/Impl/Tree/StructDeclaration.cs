using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class StructDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(Attributes);
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;
    public override PartKind TypePartKind => PartKind.Struct;
  }
}
