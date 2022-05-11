using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpProvidedTypeElement<T> where T : ITypeMember, IFSharpTypeElement
  {
    private ProvidedType Type { get; }
    private T TypeElement { get; }
    public XmlNode GetXmlDoc() => Type.GetXmlDoc(TypeElement); //TODO: should be not null

    public FSharpProvidedTypeElement(ProvidedType type, [NotNull] T typeElement)
    {
      Type = type;
      TypeElement = typeElement;
    }

    public IEnumerable<IOperator> Operators => EmptyList<IOperator>.InstanceList;

    public IEnumerable<IConstructor> Constructors => Type
      .GetConstructors()
      .Select(t => new FSharpProvidedConstructor(t, TypeElement));

    private IEnumerable<IMethod> FilterMethods(IEnumerable<IMethod> methodInfos)
    {
      var methodGroups = methodInfos.ToDictionary(t => t.XMLDocId);

      foreach (var property in Properties)
      {
        if (property.IsReadable && property.Getter is { } getter)
          methodGroups.Remove(XMLDocUtil.GetTypeMemberXmlDocId(getter, getter.ShortName));

        if (property.IsWritable && property.Setter is { } setter)
          methodGroups.Remove(XMLDocUtil.GetTypeMemberXmlDocId(setter, setter.ShortName));
      }

      foreach (var @event in Events)
      {
        if (@event.Adder is { } adder)
          methodGroups.Remove(XMLDocUtil.GetTypeMemberXmlDocId(adder, adder.ShortName));

        if (@event.Remover is { } remover)
          methodGroups.Remove(XMLDocUtil.GetTypeMemberXmlDocId(remover, remover.ShortName));

        if (@event.Raiser is { } raiser)
          methodGroups.Remove(XMLDocUtil.GetTypeMemberXmlDocId(raiser, raiser.ShortName));
      }

      return methodGroups.Values;
    }

    public IList<ITypeParameter> AllTypeParameters => EmptyList<ITypeParameter>.InstanceList;

    public IEnumerable<IMethod> Methods =>
      FilterMethods(Type.GetMethods().Select(t => new FSharpProvidedMethod(t, TypeElement)));

    public IEnumerable<IProperty> Properties =>
      Type
        .GetProperties()
        .Select(t => new FSharpProvidedProperty(t, TypeElement));

    public IEnumerable<IEvent> Events =>
      Type
        .GetEvents()
        .Select(t => new FSharpProvidedEvent(t, TypeElement));

    public IList<ITypeElement> NestedTypes =>
      Type.GetNestedTypes()
        .Select(t => (ITypeElement)new FSharpProvidedNestedClass(t, TypeElement.Module, TypeElement))
        .ToList();

    public IEnumerable<ITypeMember> GetMembers() =>
      Methods.Cast<ITypeMember>()
        .Union(Properties)
        .Union(Events)
        .Union(Fields)
        .Union(Constructors)
        .Union(NestedTypes.Cast<ITypeMember>())
        .ToList();

    public IList<IDeclaredType> GetSuperTypes() => CalculateSuperTypes().ToList();
    public IList<ITypeElement> GetSuperTypeElements() => GetSuperTypes().ToTypeElements();
    public IEnumerable<IField> Constants => Fields.Where(t => t.IsConstant);

    public IEnumerable<IField> Fields =>
      Type.GetFields()
        .Select(t => new FSharpProvidedField(t, TypeElement))
        .ToList();

    public MemberPresenceFlag GetMemberPresenceFlag()
    {
      var memberFlags = MemberPresenceFlag.NONE;

      foreach (var constructor in Constructors)
      {
        if (constructor.IsDefault)
          memberFlags |= MemberPresenceFlag.PUBLIC_DEFAULT_INSTANCE_CTOR_ALL_FLAGS;

        else if (constructor.IsParameterless)
          memberFlags |= MemberPresenceFlag.ACCESSIBLE_INSTANCE_CTOR_WITH_PARAMETERS;
      }

      if (NestedTypes.Any())
        memberFlags |= MemberPresenceFlag.ACCESSIBLE_NESTED_TYPES;

      if (Constants.Any())
        memberFlags |= MemberPresenceFlag.ACCESSIBLE_CONSTANTS;

      return memberFlags;
    }

    public IEnumerable<string> MemberNames => GetMembers().Select(t => t.ShortName); //ToHashSet?

    public IDeclaredType GetBaseClassType() =>
      Type.BaseType?.MapType(TypeElement.Module) as IDeclaredType ?? TypeElement.Module.GetPredefinedType().Object;

    public IClass GetSuperClass() => GetBaseClassType().GetClassType();

    //TODO: common & hashset
    public bool HasMemberWithName(string shortName, bool ignoreCase)
    {
      var comparisonRule = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

      foreach (var name in MemberNames)
        if (string.Equals(name, shortName, comparisonRule))
          return true;

      return false;
    }

    private IEnumerable<IDeclaredType> CalculateSuperTypes()
    {
      yield return GetBaseClassType();
      foreach (var type in Type.GetInterfaces())
      {
        if (type.MapType(TypeElement.Module) is IDeclaredType declType)
          yield return declType;
      }
    }
  }

  public class FSharpProvidedNestedClass : FSharpGeneratedElementBase, IClass, IFSharpTypeElement,
    IFSharpTypeParametersOwner, ISecondaryDeclaredElement
  {
    public FSharpProvidedNestedClass(ProvidedType type, IPsiModule module, ITypeElement containingType = null)
    {
      Assertion.Assert(!type.IsInterface);
      Module = module;
      Type = type;
      myContainingType = containingType;
      myTypeElement = new FSharpProvidedTypeElement<FSharpProvidedNestedClass>(type, this);
    }

    private ProvidedType Type { get; }
    private readonly ITypeElement myContainingType;
    private readonly FSharpProvidedTypeElement<FSharpProvidedNestedClass> myTypeElement;
    public override ITypeElement GetContainingType() => ContainingType;
    public override ITypeMember GetContainingTypeMember() => ContainingType as ITypeMember;
    public override DeclaredElementType GetElementType() => CLRDeclaredElementType.CLASS;
    public override bool IsVisibleFromFSharp => true;

    public override string ShortName => Type.Name;
    public override bool IsValid() => OriginElement is { } x && x.IsValid();

    public IClrTypeName GetClrName() =>
      Type is ProxyProvidedTypeWithContext x ? x.GetClrName() : EmptyClrTypeName.Instance;

    public IList<IDeclaredType> GetSuperTypes() => myTypeElement.GetSuperTypes();
    public IList<ITypeElement> GetSuperTypeElements() => myTypeElement.GetSuperTypeElements();
    public MemberPresenceFlag GetMemberPresenceFlag() => myTypeElement.GetMemberPresenceFlag();

    public INamespace GetContainingNamespace() => ((ITypeElement)OriginElement).GetContainingNamespace();

    public IPsiSourceFile GetSingleOrDefaultSourceFile() => null;

    public bool HasMemberWithName(string shortName, bool ignoreCase) =>
      myTypeElement.HasMemberWithName(shortName, ignoreCase);

    public IEnumerable<ITypeMember> GetMembers() => myTypeElement.GetMembers();
    public IEnumerable<IConstructor> Constructors => myTypeElement.Constructors;
    public IEnumerable<IOperator> Operators => myTypeElement.Operators;
    public IEnumerable<IMethod> Methods => myTypeElement.Methods;
    public IEnumerable<IProperty> Properties => myTypeElement.Properties;
    public IEnumerable<IEvent> Events => myTypeElement.Events;
    public IEnumerable<string> MemberNames => myTypeElement.MemberNames;
    public IEnumerable<IField> Constants => myTypeElement.Constants;
    public IEnumerable<IField> Fields => myTypeElement.Fields;
    public IList<ITypeElement> NestedTypes => myTypeElement.NestedTypes;

    public IClrDeclaredElement OriginElement
    {
      get
      {
        var declaringType = Type.DeclaringType;
        while (declaringType.DeclaringType != null)
          declaringType = declaringType.DeclaringType;

        return declaringType.MapType(Module).GetTypeElement();
      }
    }

    public bool IsReadOnly => true;

    protected override IClrDeclaredElement ContainingElement => ContainingType;
    public IList<ITypeParameter> AllTypeParameters => TypeParameters;
    public IList<ITypeParameter> TypeParameters => EmptyList<ITypeParameter>.Instance;

    public AccessRights GetAccessRights() =>
      Type.IsPublic || Type.IsNestedPublic ? AccessRights.PUBLIC : AccessRights.PRIVATE;

    public bool IsAbstract => Type.IsAbstract;
    public bool IsSealed => Type.IsSealed;
    public bool IsVirtual => false;
    public bool IsOverride => false;
    public bool IsStatic => true;
    public bool IsReadonly => false;
    public bool IsExtern => false;
    public bool IsUnsafe => false;
    public bool IsVolatile => false;
    public string XMLDocId => XMLDocUtil.GetTypeElementXmlDocId(this);
    public IList<TypeMemberInstance> GetHiddenMembers() => EmptyList<TypeMemberInstance>.Instance;
    public Hash? CalcHash() => null;
    public ITypeElement ContainingType => myContainingType ?? Type.DeclaringType.MapType(Module).GetTypeElement();
    public AccessibilityDomain AccessibilityDomain => new(AccessibilityDomain.AccessibilityDomainType.PUBLIC, null);
    public MemberHidePolicy HidePolicy => MemberHidePolicy.HIDE_BY_NAME;
    public IDeclaredType GetBaseClassType() => myTypeElement.GetBaseClassType();
    public IClass GetSuperClass() => myTypeElement.GetSuperClass();
    public override ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;
    public override IPsiModule Module { get; }
    public override IPsiServices GetPsiServices() => Module.GetPsiServices();
    public new XmlNode GetXMLDoc(bool inherit) => myTypeElement.GetXmlDoc();

    public override bool Equals(object obj) =>
      obj switch
      {
        FSharpProvidedNestedClass x => ProvidedTypesComparer.Instance.Equals(x.Type, Type),
        _ => false
      };

    public override int GetHashCode() => ProvidedTypesComparer.Instance.GetHashCode(Type);
  }
}
