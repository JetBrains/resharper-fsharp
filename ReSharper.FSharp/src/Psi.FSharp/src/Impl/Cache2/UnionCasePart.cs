using System.Collections.Generic;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class UnionCasePart : FSharpClassLikePart<IUnionCaseDeclaration>, Class.IClassPart
  {
    public UnionCasePart(IUnionCaseDeclaration declaration, ICacheBuilder cacheBuilder)
      : base(declaration, ModifiersUtil.GetDecoration(declaration),
        TreeNodeCollection<ITypeParameterOfTypeDeclaration>.Empty, cacheBuilder)
    {
    }

    public UnionCasePart(IReader reader) : base(reader)
    {
    }

    public override IEnumerable<IDeclaredType> GetSuperTypes()
    {
      var type = (GetDeclaration()?.GetContainingNode<IUnionDeclaration>() as ITypeDeclaration)?.DeclaredElement;
      return type != null ? new[] {TypeFactory.CreateType(type)} : EmptyList<IDeclaredType>.InstanceList;
    }

    public override TypeElement CreateTypeElement()
    {
      var isSingleton = GetDeclaration()?.Fields.IsEmpty ?? true;
      return new FSharpUnionCase(this);
    }

    public override MemberDecoration Modifiers =>
      GetDeclaration()?.Fields.IsEmpty ?? false
        ? MemberDecoration.FromModifiers(Psi.Modifiers.INTERNAL)
        : base.Modifiers;


    public MemberPresenceFlag GetMemberPresenceFlag()
    {
      return MemberPresenceFlag.INSTANCE_CTOR;
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.UnionCase;
  }
}