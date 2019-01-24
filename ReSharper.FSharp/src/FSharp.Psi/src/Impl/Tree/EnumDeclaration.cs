using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class EnumDeclaration
  {
    protected override string DeclaredElementName => Identifier.GetCompiledName(Attributes);
    public override IFSharpIdentifier NameIdentifier => Identifier;

    public override IReadOnlyList<ITypeMemberDeclaration> MemberDeclarations =>
      EnumMembers.Cast<ITypeMemberDeclaration, IEnumMemberDeclaration>();
  }
}