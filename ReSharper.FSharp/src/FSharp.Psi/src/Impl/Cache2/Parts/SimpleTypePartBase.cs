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
    protected SimpleTypePartBase([NotNull] IFSharpTypeOrExtensionDeclaration declaration,
      [NotNull] ICacheBuilder cacheBuilder) : base(declaration, cacheBuilder, 
      FSharpGeneratedMembers.SimpleTypeExtendsListShortNames)
    {
    }

    protected SimpleTypePartBase(IReader reader) : base(reader)
    {
    }

    public override MemberPresenceFlag GetMemberPresenceFlag() =>
      base.GetMemberPresenceFlag() |
      MemberPresenceFlag.INSTANCE_CTOR |
      MemberPresenceFlag.EXPLICIT_OP | MemberPresenceFlag.IMPLICIT_OP |
      MemberPresenceFlag.MAY_EQUALS_OVERRIDE;

    public override IDeclaredType GetBaseClassType() =>
      GetPsiModule().GetPredefinedType().Object;

    public override IEnumerable<ITypeMember> GetTypeMembers() =>
      base.GetTypeMembers().Prepend(this.GetGeneratedMembers());

    public bool OverridesToString => true;
    public bool HasCompareTo => true;
  }

  public interface ISimpleTypePart : ClassLikeTypeElement.IClassLikePart
  {
    bool OverridesToString { get; }
    bool HasCompareTo { get; }
  }
}
