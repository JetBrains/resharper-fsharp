using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement.CompilerGenerated
{
  internal class FSharpUnionTagsClass : FSharpGeneratedMemberBase, IClass
  {
    private class TagField : FSharpGeneratedMemberBase, IField
    {
      private readonly ITypeElement myTagsClass;
      private readonly int myIndex;

      public TagField(string name, [NotNull] IClass containingType, ITypeElement tagsClass, int index)
        : base(containingType)
      {
        myTagsClass = tagsClass;
        myIndex = index;
        ShortName = name;
      }

      public override DeclaredElementType GetElementType()
      {
        return CLRDeclaredElementType.CONSTANT;
      }

      public override string ShortName { get; }
      public override MemberHidePolicy HidePolicy => MemberHidePolicy.HIDE_BY_NAME;
      public IType Type => Module.GetPredefinedType().Int;
      public ConstantValue ConstantValue => new ConstantValue(myIndex, Type);
      public bool IsField => false;
      public bool IsConstant => true;
      public bool IsEnumMember => false;
      public int? FixedBufferSize => null;

      public override bool IsStatic => true;
      public override bool IsReadonly => true;

      public override ITypeElement GetContainingType()
      {
        return myTagsClass;
      }

      public override ITypeMember GetContainingTypeMember()
      {
        return (ITypeMember) myTagsClass;
      }

      public override ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;
    }

    private const string TagsClassName = "Tags";

    [NotNull] private readonly FSharpUnion myContainingType;

    internal FSharpUnionTagsClass([NotNull] FSharpUnion containingType) : base(containingType)
    {
      myContainingType = containingType;
    }

    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.CLASS;
    }

    public override string ShortName => TagsClassName;
    public override MemberHidePolicy HidePolicy => MemberHidePolicy.HIDE_BY_NAME;
    public IList<ITypeParameter> TypeParameters => EmptyList<ITypeParameter>.Instance;

    public IClrTypeName GetClrName()
    {
      return new ClrTypeName($"{myContainingType.GetClrName().FullName}+{TagsClassName}");
    }

    public IList<IDeclaredType> GetSuperTypes()
    {
      return EmptyList<IDeclaredType>.Instance;
    }

    public IEnumerable<ITypeMember> GetMembers()
    {
      return Constants;
    }

    public INamespace GetContainingNamespace()
    {
      return myContainingType.GetContainingNamespace();
    }

    public IPsiSourceFile GetSingleOrDefaultSourceFile()
    {
      return myContainingType.GetSingleOrDefaultSourceFile();
    }

    public IList<ITypeElement> NestedTypes => EmptyList<ITypeElement>.Instance;
    public IEnumerable<IConstructor> Constructors => EmptyList<IConstructor>.Instance;
    public IEnumerable<IOperator> Operators => EmptyList<IOperator>.Instance;
    public IEnumerable<IMethod> Methods => EmptyList<IMethod>.Instance;
    public IEnumerable<IProperty> Properties => EmptyList<IProperty>.Instance;
    public IEnumerable<IEvent> Events => EmptyList<IEvent>.Instance;
    public IEnumerable<string> MemberNames => myContainingType.Cases.Select(c => c.ShortName);

    public IDeclaredType GetBaseClassType()
    {
      return null;
    }

    public MemberPresenceFlag GetMemberPresenceFlag()
    {
      return MemberPresenceFlag.NONE;
    }

    public override bool IsStatic => true;

    public IEnumerable<IField> Constants
    {
      get
      {
        var count = 0;
        var tags = new LocalList<IField>();
        foreach (var unionCase in myContainingType.Cases)
          tags.Add(new TagField(unionCase.ShortName, this, this, count++));
        return tags.ResultingList();
      }
    }

    public IEnumerable<IField> Fields => EmptyList<IField>.Enumerable;
  }
}