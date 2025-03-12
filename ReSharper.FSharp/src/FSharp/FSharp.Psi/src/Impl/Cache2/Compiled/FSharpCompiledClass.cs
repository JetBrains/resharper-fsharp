using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Access;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.Metadata.Utils;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.Reflection2;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled
{
  public class FSharpCompiledClass : FSharpCompiledClassBase
  {
    public FSharpCompiledClass(FSharpMetadataEntity entity, [NotNull] ICompiledEntity parent,
      [NotNull] IReflectionBuilder builder, [NotNull] IMetadataTypeInfo info) : base(entity, parent, builder, info)
    {
    }
  }

  public class FSharpCompiledAbbreviationTypeInfo(MetadataToken token, IMetadataAssembly assembly)
    : MetadataEntity(token, assembly), IMetadataTypeInfo
  {
    public override string ToString()
    {
      return "todo";
    }

    public string Name { get; }
    public string FullyQualifiedName { get; }
    public string AssemblyQualifiedName { get; }
    public string NamespaceName { get; }
    public string TypeName { get; }

    public AssemblyNameInfo DeclaringAssemblyName { get; }
    public IMetadataTypeInfo DeclaringType { get; }
    
    public bool IsPublic => false;
    public bool IsNotPublic => false;
    public bool IsNested => false;
    public bool IsNestedPublic => false;
    public bool IsNestedPrivate => false;
    public bool IsNestedFamily => false;
    public bool IsNestedAssembly => false;
    public bool IsNestedFamilyAndAssembly => false;
    public bool IsNestedFamilyOrAssembly => false;

    
    public IMetadataSecurityRow[] Security => throw new InvalidOperationException();
    public string[] SecurityAttributesTypeName => EmptyArray<string>.Instance;
    public bool HasSecurity => false;
    public bool IsSpecialName => false;
    public bool IsRuntimeSpecialName => false;

    public IEnumerable<MemberInfo> GetMemberInfos() => EmptyList<MemberInfo>.Instance;
    public IMetadataMethod[] GetMethods() => EmptyArray<IMetadataMethod>.Instance;
    public IMetadataField[] GetFields() => EmptyArray<IMetadataField>.Instance;
    public IMetadataProperty[] GetProperties() => EmptyArray<IMetadataProperty>.Instance;
    public IMetadataEvent[] GetEvents() => EmptyArray<IMetadataEvent>.Instance;
    public IMetadataTypeInfo[] GetNestedTypes() => EmptyArray<IMetadataTypeInfo>.Instance;

    public bool HasExtensionMethods() => false;

    public MetadataMemberPresenceFlags ComputeMemberPresenceFlag() => MetadataMemberPresenceFlags.None;

    public IMetadataProperty GetPropertyFromAccessor(IMetadataMethod accessor) => null;

    public IMetadataClassType Base => null;

    public IMetadataInterfaceImplementation[] InterfaceImplementations =>
      EmptyArray<IMetadataInterfaceImplementation>.Instance;

    public IMetadataTypeParameter[] TypeParameters => EmptyArray<IMetadataTypeParameter>.Instance;
    public bool IsAbstract => false;
    public bool IsSealed => false;
    public bool IsImported => false;
    public ClassLayoutType Layout => ClassLayoutType.Auto;
    public ClassLayout ClassLayout => ClassLayout.Empty;
    public PInvokeInfo.CharSetSpec InteropStringFormat => PInvokeInfo.CharSetSpec.NotSpecified;
    public bool IsBeforeFieldInit => false;
    public bool IsClass => false;
    public bool IsInterface => false;
    public bool IsSerializable => false;
    public bool IsWindowsRuntime => false;
    public int PackingSize => 0;
    public int ClassSize => 0;
  }

  public class FSharpCompiledAbbreviationType : CompiledTypeElement
  {
    public FSharpCompiledAbbreviationType([NotNull] ICompiledEntity parent, [NotNull] IReflectionBuilder builder, [NotNull] CompiledTypeElementFactory factory, [NotNull] IMetadataTypeInfo info) : base(parent, builder, factory, info)
    {
    }

    public override DeclaredElementType GetElementType()
    {
      throw new NotImplementedException();
    }

    public override IEnumerable<ITypeMember> GetMembers() => EmptyList<ITypeMember>.Instance;

    public override MemberPresenceFlag GetMemberPresenceFlag()
    {
      throw new NotImplementedException();
    }

    public override bool HasMemberWithName(string shortName, bool ignoreCase)
    {
      throw new NotImplementedException();
    }

    public override IList<ITypeElement> NestedTypes { get; }
    public override IEnumerable<IField> Constants { get; }
    public override IEnumerable<IField> Fields { get; }
    public override IEnumerable<IConstructor> Constructors { get; }
    public override IEnumerable<IOperator> Operators { get; }
    public override IEnumerable<IMethod> Methods { get; }
    public override IEnumerable<IProperty> Properties { get; }
    public override IEnumerable<IEvent> Events { get; }
    public override IEnumerable<string> MemberNames { get; }
  }
}
