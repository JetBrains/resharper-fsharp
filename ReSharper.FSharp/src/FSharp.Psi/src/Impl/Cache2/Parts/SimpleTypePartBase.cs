using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal abstract class SimpleTypePartBase : FSharpTypeMembersOwnerTypePart
  {
    private static readonly string[] ourExtendsListShortNames =
      {"IStructuralEquatable", "IStructuralComparable", "IComparable"};

    protected SimpleTypePartBase([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder)
    {
    }

    protected SimpleTypePartBase(IReader reader) : base(reader)
    {
    }

    public override string[] ExtendsListShortNames =>
      ArrayUtil.Add(ourExtendsListShortNames, base.ExtendsListShortNames);

    public override MemberPresenceFlag GetMemberPresenceFlag()
    {
      return base.GetMemberPresenceFlag() |
             MemberPresenceFlag.INSTANCE_CTOR |
             MemberPresenceFlag.EXPLICIT_OP | MemberPresenceFlag.IMPLICIT_OP |
             MemberPresenceFlag.MAY_EQUALS_OVERRIDE | MemberPresenceFlag.MAY_TOSTRING_OVERRIDE;
    }

    public override IDeclaredType GetBaseClassType() =>
      GetPsiModule().GetPredefinedType().Object;
  }
}