using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  internal class FSharpUnionTagsClass : FSharpGeneratedMemberBase, IClass
  {
    private const string TagsClassName = "Tags";

    public readonly TypePart TypePart;

    internal FSharpUnionTagsClass([NotNull] TypePart typePart) =>
      TypePart = typePart;

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.CLASS;

    protected IUnionPart UnionPart => (IUnionPart) TypePart;
    protected TypeElement ContainingType => TypePart.TypeElement;

    protected override IClrDeclaredElement ContainingElement => ContainingType;
    public override ITypeElement GetContainingType() => ContainingType;
    public override ITypeMember GetContainingTypeMember() => ContainingType;

    public override string ShortName => TagsClassName;
    public IList<ITypeParameter> TypeParameters => EmptyList<ITypeParameter>.Instance;

    public IClrTypeName GetClrName() =>
      new ClrTypeName($"{ContainingType.GetClrName().FullName}+{TagsClassName}");

    public IEnumerable<ITypeMember> GetMembers() => Constants;

    public IEnumerable<string> MemberNames =>
      UnionPart.Cases.Select(c => c.ShortName);

    public INamespace GetContainingNamespace() =>
      ContainingType.GetContainingNamespace();

    public IPsiSourceFile GetSingleOrDefaultSourceFile() =>
      ContainingType.GetSingleOrDefaultSourceFile();


    public override bool IsStatic => true;

    public IEnumerable<IField> Constants
    {
      get
      {
        var tags = new LocalList<IField>();
        for (var i = 0; i < UnionPart.Cases.Count; i++)
          tags.Add(new UnionCaseTag(this, i));

        return tags.ResultingList();
      }
    }

    public IDeclaredType GetBaseClassType() => PredefinedType.Object;
    public IList<IDeclaredType> GetSuperTypes() => new[] {GetBaseClassType()};

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

      if (!(obj is FSharpUnionTagsClass tags)) return false;

      return Equals(GetContainingType(), tags.GetContainingType());
    }

    public override int GetHashCode() => ShortName.GetHashCode();

    public override string XMLDocId =>
      XMLDocUtil.GetTypeElementXmlDocId(this);

    public override AccessRights GetAccessRights() =>
      ContainingType.GetRepresentationAccessRights();

    #region UnionCaseTag

    public class UnionCaseTag : FSharpGeneratedMemberBase, IField
    {
      public FSharpUnionTagsClass TagsClass { get; }
      private readonly int myIndex;

      public UnionCaseTag(FSharpUnionTagsClass tagsClass, int index)
      {
        TagsClass = tagsClass;
        myIndex = index;
      }

      public override string ShortName =>
        TagsClass.UnionPart.Cases[myIndex].ShortName;

      protected override IClrDeclaredElement ContainingElement => TagsClass;
      public override ITypeElement GetContainingType() => TagsClass;
      public override ITypeMember GetContainingTypeMember() => TagsClass;

      public override DeclaredElementType GetElementType() =>
        CLRDeclaredElementType.CONSTANT;

      public IType Type => PredefinedType.Int;
      public ConstantValue ConstantValue => new ConstantValue(myIndex, Type);
      public bool IsField => false;
      public bool IsConstant => true;
      public bool IsEnumMember => false;
      public int? FixedBufferSize => null;

      public override bool IsStatic => true;
      public override bool IsReadonly => true;

      public override ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;

      public override bool Equals(object obj)
      {
        if (ReferenceEquals(this, obj))
          return true;

        if (!(obj is UnionCaseTag tag)) return false;

        if (!ShortName.Equals(tag.ShortName))
          return false;

        return Equals(GetContainingType(), tag.GetContainingType());
      }

      public override int GetHashCode() => ShortName.GetHashCode();
    }

    #endregion
  }
}