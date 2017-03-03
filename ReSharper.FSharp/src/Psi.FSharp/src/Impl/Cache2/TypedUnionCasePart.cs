using System.Collections.Generic;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class TypedUnionCasePart : FSharpClassLikePart<IFSharpTypedUnionCaseDeclaration>, Class.IClassPart
  {
    public TypedUnionCasePart(IReader reader) : base(reader)
    {
    }

    public TypedUnionCasePart(IFSharpTypedUnionCaseDeclaration declaration)
      : base(declaration, declaration.DeclaredName, MemberDecoration.DefaultValue)
    {
    }

    public override IEnumerable<IDeclaredType> GetSuperTypes()
    {
      var type = (GetDeclaration()?.GetContainingNode<IFSharpUnionDeclaration>() as ITypeDeclaration)?.DeclaredElement;
      return type != null ? new[] {TypeFactory.CreateType(type)} : EmptyList<IDeclaredType>.InstanceList;
    }

    public override TypeElement CreateTypeElement()
    {
      return new FSharpTypedUnionCase(this);
    }

    public MemberPresenceFlag GetMemberPresenceFlag()
    {
      return MemberPresenceFlag.INSTANCE_CTOR;
    }

    protected override byte SerializationTag => (byte) FSharpSerializationTag.TypedUnionCasePart;
  }
}