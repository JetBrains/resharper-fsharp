using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal abstract class SimpleTypePartBase : FSharpTypeMembersOwnerTypePart, ISimpleTypePart
  {
    protected SimpleTypePartBase([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder)
    {
    }

    protected SimpleTypePartBase(IReader reader) : base(reader)
    {
    }

    public override string[] ExtendsListShortNames =>
      FSharpGeneratedMembers.SimpleTypeExtendsListShortNames;

    public override MemberPresenceFlag GetMemberPresenceFlag()
    {
      return base.GetMemberPresenceFlag() |
             MemberPresenceFlag.INSTANCE_CTOR |
             MemberPresenceFlag.EXPLICIT_OP | MemberPresenceFlag.IMPLICIT_OP |
             MemberPresenceFlag.MAY_EQUALS_OVERRIDE | MemberPresenceFlag.MAY_TOSTRING_OVERRIDE;
    }

    public override IDeclaredType GetBaseClassType() =>
      GetPsiModule().GetPredefinedType().Object;

    public override IEnumerable<IDeclaredType> GetSuperTypes()
    {
      var psiModule = GetPsiModule();
      var predefinedType = psiModule.GetPredefinedType();
      return new[]
      {
        predefinedType.Object,
        predefinedType.IComparable,
        predefinedType.GenericIComparable,
        predefinedType.GenericIEquatable,
        TypeFactory.CreateTypeByCLRName(FSharpGeneratedMembers.StructuralComparableInterfaceName, psiModule),
        TypeFactory.CreateTypeByCLRName(FSharpGeneratedMembers.StructuralEquatableInterfaceName, psiModule)
      };
    }

    protected virtual IList<ITypeMember> GetGeneratedMembers() =>
      GeneratedMembersUtil.GetGeneratedMembers(this);

    public override IEnumerable<ITypeMember> GetTypeMembers() =>
      GetGeneratedMembers().Prepend(base.GetTypeMembers());

    public bool OverridesToString => true;
    public bool HasCompareTo => true;
  }

  public interface ISimpleTypePart : ClassLikeTypeElement.IClassLikePart
  {
    bool OverridesToString { get; }
    bool HasCompareTo { get; }
  }
}
