using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeAbbreviationDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(AllAttributes);
    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;

    public override PartKind TypePartKind =>
      FSharpImplUtil.GetTypeKind(AllAttributes, out var typeKind) && typeKind == PartKind.Struct
        ? PartKind.Struct
        : PartKind.Class;

    public bool CanBeUnionCase =>
      AbbreviatedTypeOrUnionCase.CanBeUnionCase;
  }
}
