using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class EnumDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(Attributes);
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;

    public override IReadOnlyList<ITypeMemberDeclaration> MemberDeclarations =>
      EnumMembers.Cast<ITypeMemberDeclaration, IEnumMemberDeclaration>();
  }
}