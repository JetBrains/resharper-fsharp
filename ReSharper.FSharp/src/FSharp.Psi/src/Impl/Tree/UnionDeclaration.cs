using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class UnionDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(AllAttributes);
    public override IFSharpIdentifierLikeNode NameIdentifier => (IFSharpIdentifierLikeNode) Identifier;

    public override IReadOnlyList<ITypeMemberDeclaration> MemberDeclarations =>
      base.MemberDeclarations.Prepend(UnionCases).AsIReadOnlyList();

    public override PartKind TypePartKind =>
      FSharpImplUtil.GetTypeKind(AllAttributes, out var typeKind)
        ? typeKind
        : PartKind.Class;
  }
}
