using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal partial class FSharpEnumDeclaration
  {
    public override string DeclaredName => Identifier.GetName();

    public override TreeTextRange GetNameRange()
    {
      return Identifier.GetNameRange();
    }

    public override TreeNodeCollection<ITypeMemberDeclaration> MemberDeclarations =>
      EnumMembers.Cast<ITypeMemberDeclaration, IFSharpEnumMemberDeclaration>();
  }
}