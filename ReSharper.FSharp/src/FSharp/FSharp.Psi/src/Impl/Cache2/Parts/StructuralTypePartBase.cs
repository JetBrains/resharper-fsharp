using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal abstract class StructuralTypePartBase : FSharpTypeMembersOwnerTypePart, IFSharpStructuralTypePart
  {
    protected StructuralTypePartBase([NotNull] IFSharpTypeOrExtensionDeclaration declaration,
      [NotNull] ICacheBuilder cacheBuilder, PartKind partKind) : base(declaration, cacheBuilder, partKind, 
      FSharpGeneratedMembers.StructuralTypeExtendsListShortNames)
    {
    }

    protected StructuralTypePartBase(IReader reader) : base(reader)
    {
    }

    public override MemberPresenceFlag GetMemberPresenceFlag() =>
      base.GetMemberPresenceFlag() |
      MemberPresenceFlag.MAY_EQUALS_OVERRIDE |
      (ReportCtor ? MemberPresenceFlag.INSTANCE_CTOR : 0);

    public override IDeclaredType GetBaseClassType() =>
      GetPsiModule().GetPredefinedType().Object;

    public override IEnumerable<ITypeMember> GetTypeMembers() =>
      base.GetTypeMembers().Prepend(this.GetGeneratedMembers());

    public virtual bool OverridesToString => true;
    public virtual bool ReportCtor => true;
    public bool HasCompareTo => true;
  }

  public interface IFSharpStructuralTypePart : IFSharpTypePart, ClassLikeTypeElement.IClassLikePart
  {
    bool OverridesToString { get; }
    bool HasCompareTo { get; }
  }
}
