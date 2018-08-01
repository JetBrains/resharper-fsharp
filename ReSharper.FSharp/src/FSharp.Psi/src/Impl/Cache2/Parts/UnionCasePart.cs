using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class UnionCasePart : FSharpClassLikePart<INestedTypeUnionCaseDeclaration>, Class.IClassPart
  {
    public UnionCasePart([NotNull] INestedTypeUnionCaseDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, ModifiersUtil.GetDecoration(declaration),
        TreeNodeCollection<ITypeParameterOfTypeDeclaration>.Empty, cacheBuilder)
    {
      var unionShortName = declaration.GetContainingNode<IUnionDeclaration>()?.ShortName;
      ExtendsListShortNames =
        unionShortName != null
          ? new[] {cacheBuilder.Intern(unionShortName)}
          : EmptyArray<string>.Instance;
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteStringArray(ExtendsListShortNames);
    }

    public override string[] ExtendsListShortNames { get; }

    public override IDeclaredType GetBaseClassType()
    {
      var typeElement = TypeElement.GetContainingType();
      return typeElement != null
        ? TypeFactory.CreateType(typeElement)
        : null;
    }

    public UnionCasePart(IReader reader) : base(reader)
    {
      ExtendsListShortNames = reader.ReadStringArray();
    }

    public override IEnumerable<IDeclaredType> GetSuperTypes()
    {
      var baseType = GetBaseClassType();
      if (baseType == null)
        return EmptyList<IDeclaredType>.Instance;

      return new[] {baseType};
    }

    public override TypeElement CreateTypeElement()
    {
      return new FSharpNestedTypeUnionCase(this);
    }

    public override MemberDecoration Modifiers =>
      Parent is IUnionPart unionPart && !unionPart.HasPublicNestedTypes
        ? MemberDecoration.FromModifiers(ReSharper.Psi.Modifiers.INTERNAL)
        : base.Modifiers;

    public MemberPresenceFlag GetMemberPresenceFlag()
    {
      return MemberPresenceFlag.INSTANCE_CTOR;
    }

    protected override byte SerializationTag => (byte) FSharpPartKind.UnionCase;

    public IList<FSharpFieldProperty> CaseFields
    {
      get
      {
        var declaration = GetDeclaration();
        if (declaration == null)
          return EmptyList<FSharpFieldProperty>.Instance;

        var result = new LocalList<FSharpFieldProperty>();
        foreach (var fieldDeclaration in declaration.Fields)
        {
          if (fieldDeclaration.DeclaredElement is FSharpFieldProperty field)
            result.Add(field);
        }

        return result.ResultingList();
      }
    }
  }
}
