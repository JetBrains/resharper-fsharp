using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class EnumDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(AllAttributes);
    public override IFSharpIdentifierLikeNode NameIdentifier => (IFSharpIdentifierLikeNode) Identifier;

    public override IReadOnlyList<ITypeMemberDeclaration> MemberDeclarations =>
      EnumMembers.Cast<ITypeMemberDeclaration, IEnumMemberDeclaration>();

    public override PartKind TypePartKind => PartKind.Enum;
  }
}