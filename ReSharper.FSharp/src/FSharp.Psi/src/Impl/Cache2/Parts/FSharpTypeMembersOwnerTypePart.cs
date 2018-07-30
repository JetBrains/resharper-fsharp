using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal abstract class FSharpTypeMembersOwnerTypePart : FSharpClassLikePart<IFSharpTypeDeclaration>
  {
    protected FSharpTypeMembersOwnerTypePart([NotNull] IFSharpTypeDeclaration declaration,
      [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, ModifiersUtil.GetDecoration(declaration.AccessModifiers, declaration.AttributesEnumerable),
        declaration.TypeParameters, cacheBuilder)
    {
      var extendListShortNames = new FrugalLocalHashSet<string>();
      foreach (var member in declaration.TypeMembersEnumerable)
      {
        var baseTypeIdentifier = (member as ITypeInherit)?.BaseType?.LongIdentifier;
        if (baseTypeIdentifier != null)
        {
          extendListShortNames.Add(baseTypeIdentifier.Name);
          continue;
        }

        var interfaceImplTypeIdentifier = (member as IInterfaceImplementation)?.InterfaceType?.LongIdentifier;
        if (interfaceImplTypeIdentifier != null)
        {
          extendListShortNames.Add(interfaceImplTypeIdentifier.Name);
          continue;
        }

        var interfaceInheritTypeIdentifier = (member as IInterfaceInherit)?.InterfaceType?.LongIdentifier;
        if (interfaceInheritTypeIdentifier != null)
          extendListShortNames.Add(interfaceInheritTypeIdentifier.Name);
      }

      ExtendsListShortNames = extendListShortNames.ToArray();
    }

    protected FSharpTypeMembersOwnerTypePart(IReader reader) : base(reader)
    {
      ExtendsListShortNames = reader.ReadStringArray();
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteStringArray(ExtendsListShortNames);
    }

    public override string[] ExtendsListShortNames { get; }

    public virtual MemberPresenceFlag GetMemberPresenceFlag() =>
      MemberPresenceFlag.SIGN_OP | MemberPresenceFlag.EXPLICIT_OP |
      MemberPresenceFlag.MAY_EQUALS_OVERRIDE | MemberPresenceFlag.MAY_TOSTRING_OVERRIDE |

      // RIDER-10263
      (HasPublicDefaultCtor ? MemberPresenceFlag.PUBLIC_DEFAULT_CTOR : MemberPresenceFlag.NONE);

    public override IDeclaredType GetBaseClassType()
    {
      // todo: check inherit only
      if (ExtendsListShortNames.IsEmpty())
        return null;

      return base.GetBaseClassType();
    }

    public override IEnumerable<IDeclaredType> GetSuperTypes()
    {
      if (ExtendsListShortNames.IsEmpty())
        return EmptyList<IDeclaredType>.InstanceList;

      var declaration = GetDeclaration();
      if (declaration == null)
        return EmptyList<IDeclaredType>.InstanceList;

      return declaration.SuperTypes;
    }
  }
}
