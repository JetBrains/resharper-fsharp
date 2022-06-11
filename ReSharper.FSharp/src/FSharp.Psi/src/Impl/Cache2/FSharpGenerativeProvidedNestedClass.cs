using System.Collections.Generic;
using System.Xml;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;
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
  public class FSharpGenerativeProvidedNestedClass : FSharpGenerativeProvidedMember<ProvidedType>, IClass,
    IFSharpTypeElement
  {
    public FSharpGenerativeProvidedNestedClass(ProvidedType providedType, IPsiModule module,
      ITypeElement containingType) :
      base(providedType, containingType)
    {
      Assertion.Assert(!providedType.IsInterface);
      Module = module;
      ProvidedType = providedType;
      myMembersConverter = new GenerativeMembersConverter<FSharpGenerativeProvidedNestedClass>(providedType, this);
    }

    private ProvidedType ProvidedType { get; }
    private readonly GenerativeMembersConverter<FSharpGenerativeProvidedNestedClass> myMembersConverter;
    public override DeclaredElementType GetElementType() => CLRDeclaredElementType.CLASS;
    public override bool IsVisibleFromFSharp => true;

    public override string ShortName => ProvidedType.Name;
    public override bool IsValid() => OriginElement is { } x && x.IsValid();

    public IClrTypeName GetClrName() =>
      ProvidedType is ProxyProvidedTypeWithContext x ? x.GetClrName() : EmptyClrTypeName.Instance;

    public IList<IDeclaredType> GetSuperTypes() => myMembersConverter.GetSuperTypes();
    public IList<ITypeElement> GetSuperTypeElements() => myMembersConverter.GetSuperTypeElements();
    public MemberPresenceFlag GetMemberPresenceFlag() => myMembersConverter.GetMemberPresenceFlag();

    public INamespace GetContainingNamespace() => ((ITypeElement)OriginElement).GetContainingNamespace();

    public IPsiSourceFile GetSingleOrDefaultSourceFile() => null;

    public bool HasMemberWithName(string shortName, bool ignoreCase) =>
      myMembersConverter.HasMemberWithName(shortName, ignoreCase);

    public IEnumerable<ITypeMember> GetMembers() => myMembersConverter.GetMembers();
    public IEnumerable<IConstructor> Constructors => myMembersConverter.Constructors;
    public IEnumerable<IOperator> Operators => myMembersConverter.Operators;
    public IEnumerable<IMethod> Methods => myMembersConverter.Methods;
    public IEnumerable<IProperty> Properties => myMembersConverter.Properties;
    public IEnumerable<IEvent> Events => myMembersConverter.Events;
    public IEnumerable<string> MemberNames => myMembersConverter.MemberNames;
    public IEnumerable<IField> Constants => myMembersConverter.Constants;
    public IEnumerable<IField> Fields => myMembersConverter.Fields;
    public IList<ITypeElement> NestedTypes => myMembersConverter.NestedTypes;

    public IList<ITypeParameter> TypeParameters => EmptyList<ITypeParameter>.Instance;

    public override AccessRights GetAccessRights() =>
      ProvidedType.IsPublic || ProvidedType.IsNestedPublic ? AccessRights.PUBLIC : AccessRights.PRIVATE;

    public override bool IsAbstract => ProvidedType.IsAbstract;
    public override bool IsSealed => ProvidedType.IsSealed;
    public override string XMLDocId => XMLDocUtil.GetTypeElementXmlDocId(this);
    public IDeclaredType GetBaseClassType() => myMembersConverter.GetBaseClassType();
    public IClass GetSuperClass() => myMembersConverter.GetSuperClass();
    public override ISubstitution IdSubstitution => EmptySubstitution.INSTANCE;
    public override IPsiModule Module { get; }
    public override IPsiServices GetPsiServices() => Module.GetPsiServices();
    public override XmlNode GetXMLDoc(bool inherit) => myMembersConverter.GetXmlDoc();

    public override bool Equals(object obj) =>
      obj switch
      {
        FSharpGenerativeProvidedNestedClass x => ProvidedTypesComparer.Instance.Equals(x.ProvidedType, ProvidedType),
        _ => false
      };

    public override int GetHashCode() => ProvidedTypesComparer.Instance.GetHashCode(ProvidedType);
  }
}
