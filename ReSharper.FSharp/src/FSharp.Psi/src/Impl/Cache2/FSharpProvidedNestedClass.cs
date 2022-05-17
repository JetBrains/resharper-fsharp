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
  public class FSharpProvidedNestedClass : FSharpProvidedMember<ProvidedType>, IClass, IFSharpTypeElement
  {
    public FSharpProvidedNestedClass(ProvidedType type, IPsiModule module, ITypeElement containingType) :
      base(type, containingType)
    {
      Assertion.Assert(!type.IsInterface);
      Module = module;
      Type = type;
      myTypeElement = new FSharpProvidedTypeElement<FSharpProvidedNestedClass>(type, this);
    }

    private ProvidedType Type { get; }
    private readonly FSharpProvidedTypeElement<FSharpProvidedNestedClass> myTypeElement;
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

    public IList<ITypeParameter> TypeParameters => EmptyList<ITypeParameter>.Instance;

    public override AccessRights GetAccessRights() =>
      Type.IsPublic || Type.IsNestedPublic ? AccessRights.PUBLIC : AccessRights.PRIVATE;

    public override bool IsAbstract => Type.IsAbstract;
    public override bool IsSealed => Type.IsSealed;
    public override string XMLDocId => XMLDocUtil.GetTypeElementXmlDocId(this);
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
