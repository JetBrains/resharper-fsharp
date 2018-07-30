using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class ExceptionPart : FSharpTypeMembersOwnerTypePart, Class.IClassPart
  {
    private static readonly string[] ourExtendsListShortNames = {"Exception", "IStructuralEquatable"};

    public ExceptionPart([NotNull] IFSharpTypeDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder)
    {
    }

    public ExceptionPart(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement() =>
      new FSharpClass(this);

    protected override byte SerializationTag =>
      (byte) FSharpPartKind.Exception;

    public override string[] ExtendsListShortNames =>
      ourExtendsListShortNames;

    public override IDeclaredType GetBaseClassType() =>
      GetPsiModule().GetPredefinedType().Exception;

    public override IEnumerable<IDeclaredType> GetSuperTypes() =>
      new[]
      {
        GetPsiModule().GetPredefinedType().Exception,
        TypeFactory.CreateTypeByCLRName(FSharpGeneratedMembers.StructuralEquatableInterfaceName, GetPsiModule())
      };

    private IList<ITypeMember> GetGeneratedMembers()
    {
      var result = new LocalList<ITypeMember>();

      result.Add(new ExceptionConstructor(this));

      result.Add(new EqualsSimpleTypeMethod(TypeElement));
      result.Add(new EqualsObjectMethod(TypeElement));
      result.Add(new EqualsObjectWithComparerMethod(TypeElement));

      result.Add(new GetHashCodeMethod(TypeElement));
      result.Add(new GetHashCodeWithComparerMethod(TypeElement));

      if (GetDeclaration() is IExceptionDeclaration)
      {
        // todo: add field list tree node
        var fields = new LocalList<ITypeOwner>();
        foreach (var typeMember in base.GetTypeMembers())
          if (typeMember is FSharpFieldProperty fieldProperty)
            fields.Add(fieldProperty);

        if (!fields.IsEmpty())
          result.Add(new FSharpGeneratedConstructorFromFields(this, fields.ResultingList()));
      }

      return result.ResultingList();
    }

    public override IEnumerable<ITypeMember> GetTypeMembers() =>
      GetGeneratedMembers().Prepend(base.GetTypeMembers());
  }
}