using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class ExceptionPart : FSharpTypeMembersOwnerTypePart, IExceptionPart, IGeneratedConstructorOwner
  {
    private static readonly string[] ourExtendsListShortNames = {"Exception", "IStructuralEquatable"};

    public bool HasFields { get; }

    public ExceptionPart([NotNull] IExceptionDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder) =>
      HasFields = !declaration.Fields.IsEmpty;

    public ExceptionPart(IReader reader) : base(reader) =>
      HasFields = reader.ReadBool();

    protected override void Write(IWriter writer)
    {
      base.Write(writer);
      writer.WriteBool(HasFields);
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
        FSharpPredefinedType.StructuralEquatableTypeName.CreateTypeByClrName(GetPsiModule())
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
          if (typeMember is FSharpUnionCaseField<ExceptionFieldDeclaration> fieldProperty)
            fields.Add(fieldProperty);
        return fields.ResultingList();
      }
    }

    public bool OverridesToString => false;
    public bool HasCompareTo => false;

    public IParametersOwner GetConstructor() =>
      new FSharpGeneratedConstructorFromFields(this);
  }

  public interface IExceptionPart : Class.IClassPart, IFieldsOwnerPart
  {
    bool HasFields { get; }
  }
}
