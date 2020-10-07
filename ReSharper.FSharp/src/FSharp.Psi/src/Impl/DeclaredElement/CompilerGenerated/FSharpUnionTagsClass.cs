using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public class FSharpUnionTagsClass : FSharpGeneratedMemberBase, IClass
  {
    private const string TagsClassName = "Tags";

    public readonly TypeElement Union;

    internal FSharpUnionTagsClass([CanBeNull] TypeElement typeElement) =>
      Union = typeElement;

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.CLASS;

    internal IUnionPart UnionPart =>
      (IUnionPart) Union.EnumerateParts().FirstOrDefault(p => p is IUnionPart);

    protected override IClrDeclaredElement ContainingElement => Union;
    public override ITypeElement GetContainingType() => Union;
    public override ITypeMember GetContainingTypeMember() => Union;

    public override string ShortName => TagsClassName;
    public IList<ITypeParameter> TypeParameters => EmptyList<ITypeParameter>.Instance;

    public IClrTypeName GetClrName() =>
      new ClrTypeName($"{Union.GetClrName().FullName}+{TagsClassName}");

    public IEnumerable<ITypeMember> GetMembers() => Constants;

    public IEnumerable<string> MemberNames =>
      UnionPart.CaseDeclarations.Select(c => c.CompiledName);

    public INamespace GetContainingNamespace() =>
      Union.GetContainingNamespace();

    public IPsiSourceFile GetSingleOrDefaultSourceFile() =>
      Union.GetSingleOrDefaultSourceFile();

    public override bool IsStatic => true;

    public IEnumerable<IField> Constants
    {
      get
      {
        var tags = new List<IField>();
        foreach (var unionCase in UnionPart.Cases)
          tags.Add(new FSharpUnionCaseTag(unionCase));

        return tags;
      }
    }

    public IDeclaredType GetBaseClassType() => PredefinedType.Object;
    public IClass GetSuperClass() => GetBaseClassType().GetClassType();

    public IList<IDeclaredType> GetSuperTypes() => new[] {GetBaseClassType()};
    public IList<ITypeElement> GetSuperTypeElements() => GetSuperTypes().ToTypeElements();

    public MemberPresenceFlag GetMemberPresenceFlag() =>
      MemberPresenceFlag.NONE;

    public IEnumerable<IField> Fields => EmptyList<IField>.Instance;
    public IList<ITypeElement> NestedTypes => EmptyList<ITypeElement>.Instance;
    public IEnumerable<IConstructor> Constructors => EmptyList<IConstructor>.Instance;
    public IEnumerable<IOperator> Operators => EmptyList<IOperator>.Instance;
    public IEnumerable<IMethod> Methods => EmptyList<IMethod>.Instance;
    public IEnumerable<IProperty> Properties => EmptyList<IProperty>.Instance;
    public IEnumerable<IEvent> Events => EmptyList<IEvent>.Instance;

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(this, obj))
        return true;

      return obj is FSharpUnionTagsClass tags && Equals(GetContainingType(), tags.GetContainingType());
    }

    public override int GetHashCode() => Union.GetHashCode();

    public override string XMLDocId =>
      XMLDocUtil.GetTypeElementXmlDocId(this);

    public override AccessRights GetAccessRights() =>
      Union.GetRepresentationAccessRights();
  }
}
