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
  internal class ExceptionPart : FSharpTypeMembersOwnerTypePart, IExceptionPart
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

    public override IEnumerable<ITypeMember> GetTypeMembers() =>
      this.GetGeneratedMembers().Prepend(base.GetTypeMembers());

    public IList<ITypeOwner> Fields
    {
      get
      {
        // todo: add field list tree node
        var fields = new LocalList<ITypeOwner>();
        foreach (var typeMember in base.GetTypeMembers())
          if (typeMember is FSharpFieldProperty fieldProperty)
            fields.Add(fieldProperty);
        return fields.ResultingList();
      }
    }

    public bool OverridesToString => false;
    public bool HasCompareTo => false;
  }

  public interface IExceptionPart : Class.IClassPart, ISimpleTypePart
  {
    IList<ITypeOwner> Fields { get; }
  }
}