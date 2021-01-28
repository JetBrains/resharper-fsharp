using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class FSharpTypeDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(Attributes);
    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;

    public override IReadOnlyList<ITypeMemberDeclaration> MemberDeclarations =>
      TypeRepresentation is { } repr
        ? base.MemberDeclarations.Prepend(repr.GetMemberDeclarations()).ToIReadOnlyList()
        : base.MemberDeclarations;

    public override PartKind TypePartKind =>
      TypeRepresentation is { } repr
        ? repr.TypePartKind
        : this.GetTypeKind();

    public bool IsPrimary =>
      TypeKeyword?.GetTokenType() == FSharpTokenType.TYPE;
  }
}
