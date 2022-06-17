using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class GenerativeMembersConverter<T> where T : ITypeMember, IFSharpTypeElement
  {
    private ProvidedType Type { get; }
    private T TypeElement { get; }

    public GenerativeMembersConverter(ProvidedType type, [NotNull] T typeElement)
    {
      Type = type;
      TypeElement = typeElement;
    }

    public XmlNode GetXmlDoc() => Type.GetXmlDoc(TypeElement);

    // Operators for provided types are compiled into methods
    public IEnumerable<IOperator> Operators => EmptyList<IOperator>.InstanceList;

    public IEnumerable<IConstructor> Constructors => Type
      .GetConstructors()
      .Select(t => new FSharpGenerativeProvidedConstructor(t, TypeElement));

    public IEnumerable<IMethod> Methods => myMethods ??=
      FilterMethods(Type.GetMethods().Select(t => new FSharpGenerativeGenerativeProvidedMethod(t, TypeElement))).ToList();

    public IEnumerable<IProperty> Properties => Type
      .GetProperties()
      .Select(t => new FSharpGenerativeProvidedProperty(t, TypeElement));

    public IEnumerable<IEvent> Events => Type
      .GetEvents()
      .Select(t => new FSharpGenerativeProvidedEvent(t, TypeElement));

    public IList<ITypeElement> NestedTypes => Type
      .GetNestedTypes()
      .Select(t => (ITypeElement)new FSharpGenerativeProvidedNestedClass(t, TypeElement.Module, TypeElement))
      .ToList();

    public IEnumerable<ITypeMember> GetMembers() =>
      Methods.Cast<ITypeMember>()
        .Union(Properties)
        .Union(Events)
        .Union(Fields)
        .Union(Constructors)
        .Union(NestedTypes.OfType<ITypeMember>())
        .ToList();

    public IEnumerable<IField> Fields =>
      Type.GetFields()
        .Select(t => new FSharpGenerativeProvidedField(t, TypeElement))
        .ToList();

    public IList<IDeclaredType> GetSuperTypes() => CalculateSuperTypes().ToList();
    public IList<ITypeElement> GetSuperTypeElements() => GetSuperTypes().ToTypeElements();
    public IEnumerable<IField> Constants => Fields.Where(t => t.IsConstant);

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

      if (Type.GetNestedTypes().Any())
        memberFlags |= MemberPresenceFlag.ACCESSIBLE_NESTED_TYPES;

      if (Constants.Any())
        memberFlags |= MemberPresenceFlag.ACCESSIBLE_CONSTANTS;

      return memberFlags;
    }

    public IEnumerable<string> MemberNames => myMemberNames ??= GetMembers()
      .Where(t => t is not IConstructor)
      .Select(t => t.ShortName)
      .ToArray();

    public IDeclaredType GetBaseClassType()
    {
      if (Type.BaseType == null) return TypeElement.Module.GetPredefinedType().Object;
      return Type.BaseType.MapType(TypeElement.Module) as IDeclaredType ??
             TypeFactory.CreateUnknownType(TypeElement.Module);
    }

    public IClass GetSuperClass() => GetBaseClassType().GetClassType();

    public bool HasMemberWithName(string shortName, bool ignoreCase) =>
      SharedImplUtil.HasMemberWithName(myMemberNames, shortName, ignoreCase);

    private IEnumerable<IDeclaredType> CalculateSuperTypes()
    {
      yield return GetBaseClassType();
      foreach (var type in Type.GetInterfaces())
      {
        if (type.MapType(TypeElement.Module) is IDeclaredType declType)
          yield return declType;
      }
    }

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

    private List<IMethod> myMethods;
    private string[] myMemberNames;
  }
}
