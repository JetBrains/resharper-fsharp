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
  internal class UnionCasePart : FSharpClassLikePart<IUnionCaseDeclaration>, Class.IClassPart
  {
    private readonly bool myIsHiddenCase;

    public UnionCasePart([NotNull] IUnionCaseDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder,
      bool isHiddenCase) : base(declaration, ModifiersUtil.GetDecoration(declaration),
      TreeNodeCollection<ITypeParameterOfTypeDeclaration>.Empty, cacheBuilder)
    {
      myIsHiddenCase = isHiddenCase;
    }

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteBool(myIsHiddenCase);
    }

    public UnionCasePart(IReader reader) : base(reader)
    {
      myIsHiddenCase = reader.ReadBool();
    }

    public override IEnumerable<IDeclaredType> GetSuperTypes()
    {
      var type = (GetDeclaration()?.GetContainingNode<IUnionDeclaration>() as ITypeDeclaration)?.DeclaredElement;
      return type != null ? new[] {TypeFactory.CreateType(type)} : EmptyList<IDeclaredType>.InstanceList;
    }

    public override TypeElement CreateTypeElement()
    {
      return new FSharpUnionCase(this);
    }

    public override MemberDecoration Modifiers =>
      myIsHiddenCase || (GetDeclaration()?.Fields.IsEmpty ?? false)
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